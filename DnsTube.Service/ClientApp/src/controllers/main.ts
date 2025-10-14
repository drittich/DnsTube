import '@picocss/pico'
import 'three-dots/dist/three-dots.css'
import '../style.css'

import { format, parseISO, isValid, intlFormatDistance } from 'date-fns'
import linkifyHtml from "linkify-html";
import "linkify-plugin-ip";
import { deleteLogAsync, getLogAsync } from '../services/log';
import { getRunInfoAsync, getSettingsAsync, saveDomainsAsync, getNetworkAdapters } from '../services/settings';
import { getIp } from '../services/ip';
import { getDnsEntriesAsync, updateDnsAsync } from '../services/dns';
import { Settings } from '../model/Settings';
import { SelectedDomain } from '../model/SelectedDomain';
// import { NetworkAdapter } from '../model/NetworkAdapter';

let _settings: Settings | null = null;

/**
 * Checks if an IPv4 address is private/internal
 * Private ranges: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 127.0.0.0/8
 */
function isPrivateIPv4(ip: string): boolean {
	const parts = ip.split('.').map(Number);
	if (parts.length !== 4 || parts.some(isNaN)) return false;
	
	// 10.0.0.0/8
	if (parts[0] === 10) return true;
	
	// 172.16.0.0/12
	if (parts[0] === 172 && parts[1] >= 16 && parts[1] <= 31) return true;
	
	// 192.168.0.0/16
	if (parts[0] === 192 && parts[1] === 168) return true;
	
	// 127.0.0.0/8 (loopback)
	if (parts[0] === 127) return true;
	
	return false;
}

/**
 * Checks if an IPv6 address is private/internal
 * Private ranges: fc00::/7 (ULA), fe80::/10 (link-local), ::1/128 (loopback)
 */
function isPrivateIPv6(ip: string): boolean {
	const lower = ip.toLowerCase();
	
	// ::1 (loopback)
	if (lower === '::1') return true;
	
	// fc00::/7 (ULA - Unique Local Addresses)
	if (lower.startsWith('fc') || lower.startsWith('fd')) return true;
	
	// fe80::/10 (link-local)
	if (lower.startsWith('fe80:')) return true;
	
	return false;
}

init();

async function init() {
	// get old log right away
	getLog();

	try {
		_settings = await getSettingsAsync();

		setUiElementState();

		getLastPublicIp();

		document.getElementById('entries-refetch')?.classList.add("hidden");
		await getSelectedDnsEntries();
		document.getElementById('entries-refetch')?.classList.remove("hidden");

		// refresh log to show DNS entry fetch status
		getLog();
		setupSse();
		getRunInfo();
	} catch (error) {
		console.error(error);
	}

	getPublicIp();

	//set a timer to update nextUpdateRelative every second
	setInterval(() => {
		let nextUpdate = document.getElementById('nextUpdate') as HTMLInputElement;
		let nextUpdateDate = new Date(nextUpdate.value);
		if (isValid(nextUpdateDate)) {
			let nextUpdateRelative = document.getElementById('nextUpdateRelative')!;
			nextUpdateRelative.innerHTML = intlFormatDistance(nextUpdateDate, new Date());
		}

		let lastUpdate = document.getElementById('lastUpdate') as HTMLInputElement;
		let lastUpdateDate = new Date(lastUpdate.value);
		if (isValid(lastUpdateDate)) {
			let lastUpdateRelative = document.getElementById('lastUpdateRelative')!;
			lastUpdateRelative.innerHTML = intlFormatDistance(lastUpdateDate, new Date());
		}
	}, 1000);
}


async function getLog(lastId?: number): Promise<void> {
	let pageSize = 10;
	let isAppending = lastId != null;
	let logEntries = await getLogAsync(pageSize, lastId);

	let tableBodyEl = document.getElementById("log-table-body") as HTMLTableSectionElement;

	if (!isAppending)
		tableBodyEl.innerHTML = "";

	logEntries.forEach((entry) => {
		let color: string;
		if (entry.logLevelText == "Error")
			color = "text-danger";
		else if (entry.logLevelText == "Warning")
			color = "text-warning";
		else
			color = "text-body";

		let row = document.createElement('tr');
		row.setAttribute("data-id", entry.id!.toString());
		row.classList.add(color);

		row.insertCell().innerHTML = format(parseISO(entry.created!), "yyyy-MM-dd HH:mm:ss");
		row.insertCell().innerHTML = `<span class="word-break">${linkifyHtml(entry.text!)}</span>`;
		row.insertCell().innerHTML = entry.logLevelText!;
		tableBodyEl.appendChild(row);
	});

	document.getElementById("log-clear")!.classList.toggle("hidden", tableBodyEl.rows.length == 0);
	document.getElementById("log-load-more")!.classList.toggle("hidden", logEntries.length != pageSize);
}

document.getElementById('log-clear')!.addEventListener('click', async function (e) {
	e.preventDefault();

	if (!confirm("Are you sure you want to clear the log?"))
		return;

	let msg = await deleteLogAsync();

	if (msg == "ok") {
		let tableBodyEl = document.getElementById("log-table-body") as HTMLTableSectionElement;
		tableBodyEl.innerHTML = "";
		document.getElementById("log-clear")!.classList.add("hidden");
		document.getElementById("log-load-more")!.classList.add("hidden");
	}
	else
		alert(msg);
}, false);

document.getElementById('log-load-more')!.addEventListener('click', function (e) {
	e.preventDefault();

	let lastId = document.querySelector("#log-table-body tr:last-child")?.getAttribute("data-id");
	getLog(parseInt(lastId!));
}, false);

document.getElementById('dnsUpdate')!.addEventListener('click', async function (e) {
	e.preventDefault();

	document.getElementById('ipAddressInfoSpinner')?.classList.remove("hidden");
	document.getElementById('ipAddressInfo')?.classList.add("hidden");

	await updateDnsAsync();
}, false);

document.getElementById('entries-refetch')!.addEventListener('click', async function (e) {
	e.preventDefault();

	document.getElementById('entries-refetch')?.classList.add("hidden");
	await getSelectedDnsEntries();
	document.getElementById('entries-refetch')?.classList.remove("hidden");
}, false);

async function getLastPublicIp() {
	if (_settings == null)
		return;

	if (_settings.publicIpv4Address != null && _settings.publicIpv4Address != "")
		(document.getElementById("public-ipv4") as HTMLInputElement)!.innerHTML = _settings.publicIpv4Address!;
	if (_settings.publicIpv6Address != null && _settings.publicIpv6Address != "")
		(document.getElementById("public-ipv6") as HTMLInputElement)!.innerHTML = _settings.publicIpv6Address!;
}

async function getPublicIp() {
	let ip = await getIp();

	if (ip == null)
		return;

	if (ip.ipv4 != null && ip.ipv4 != "")
		(document.getElementById("public-ipv4") as HTMLInputElement)!.value = ip.ipv4!;
	if (ip.ipv6 != null && ip.ipv6 != "")
		(document.getElementById("public-ipv6") as HTMLInputElement)!.value = ip.ipv6!;
}

async function getSelectedDnsEntries() {
	let spinnerEl = document.getElementById("dnsEntriesSpinner") as HTMLDivElement;
	let dnsEntriesContainerEl = document.getElementById("dnsEntriesContainer") as HTMLTableElement;
	let dnsUpdateEl = document.getElementById("dnsUpdate") as HTMLAnchorElement;

	// show spinner, hide some other elements
	spinnerEl.classList.remove("hidden");
	dnsEntriesContainerEl.classList.add("hidden");
	dnsUpdateEl.classList.add("hidden");

	//remove existing rows
	let tableBodyEl = document.getElementById("dnsEntriesBody") as HTMLTableSectionElement;
	tableBodyEl.innerHTML = "";

	//TODO: if dnsEntries is null, show error message
	let dnsEntries = await getDnsEntriesAsync();
	let adapters = await getNetworkAdapters();
	let customTtl: boolean = false;
	dnsEntries!.forEach((entry, i) => {
		let row = document.createElement('tr');

		let checkbox = document.createElement('input');
		checkbox.type = "checkbox";
		checkbox.name = `dns-entry-update${i}`;
		checkbox.value = "yes";
		checkbox.id = `dns-entry-update${i}`;
		checkbox.classList.add("dns-entry-update");
		checkbox.checked = _settings!.selectedDomains!.some(function (domain) {
			return domain.dnsName == entry.dnsName && domain.type == entry.type && domain.zoneName == entry.zoneName;
		});
		checkbox.setAttribute("data-zone-name", entry.zoneName!);
		checkbox.setAttribute("data-dns-name", entry.dnsName!);
		checkbox.setAttribute("data-dns-type", entry.type!);
		checkbox.addEventListener('change', function (e) {
			e.preventDefault();

			saveDnsUpdatable();
		});
		row.insertCell().appendChild(checkbox);
		row.insertCell().innerHTML = entry.type!;
		row.insertCell().innerHTML = linkifyHtml(entry.dnsName!);
		
		// Address column with truncation and More/Less toggle
		const addressCell = row.insertCell();
		const maxLength = 75;
		const fullAddress = entry.address!;
		const linkedAddress = linkifyHtml(fullAddress);

		if (fullAddress.length > maxLength) {
			const truncated = fullAddress.substring(0, maxLength);
			
			addressCell.innerHTML = `
				<span class="address-content">
					<span class="address-truncated">${linkifyHtml(truncated)}...</span>
					<span class="address-full">${linkedAddress}</span>
					<a class="address-more-link" data-expanded="false">More</a>
				</span>
			`;
			
			// Add click handler for More/Less toggle
			const moreLink = addressCell.querySelector('.address-more-link') as HTMLAnchorElement;
			moreLink.addEventListener('click', (e) => {
				e.preventDefault();
				const isExpanded = moreLink.getAttribute('data-expanded') === 'true';
				const truncatedSpan = addressCell.querySelector('.address-truncated') as HTMLSpanElement;
				const fullSpan = addressCell.querySelector('.address-full') as HTMLSpanElement;
				
				if (isExpanded) {
					truncatedSpan.classList.remove('hide');
					fullSpan.classList.remove('show');
					moreLink.textContent = 'More';
					moreLink.setAttribute('data-expanded', 'false');
				} else {
					truncatedSpan.classList.add('hide');
					fullSpan.classList.add('show');
					moreLink.textContent = 'Less';
					moreLink.setAttribute('data-expanded', 'true');
				}
			});
		} else {
			addressCell.innerHTML = `<span class="word-break">${linkedAddress}</span>`;
		}
		
		// IP Source dropdown cell
		let adapterSelect = document.createElement('select');
		adapterSelect.name = `dns-entry-adapter${i}`;
		adapterSelect.id = `dns-entry-adapter${i}`;
		adapterSelect.classList.add("dns-entry-adapter");
		adapterSelect.setAttribute("data-zone-name", entry.zoneName!);
		adapterSelect.setAttribute("data-dns-name", entry.dnsName!);
		adapterSelect.setAttribute("data-dns-type", entry.type!);
		
		// Improved logic:
		// 1. Enable dropdown for A/AAAA records (to allow selecting from multiple public interfaces)
		// 2. For proxied records: exclude private IP adapters from dropdown (Cloudflare limitation)
		// 3. For non-proxied records: include all adapters
		// 4. Disable dropdown for non A/AAAA types (MX, NS, etc.)
		const normalizedType = (entry.type || '').toString().trim().toUpperCase();
		const isAorAAAA = normalizedType === 'A' || normalizedType === 'AAAA';
		const isProxied = entry.proxied === true;

		// Attach attributes for validation
		adapterSelect.setAttribute('data-type', entry.type || '');
		adapterSelect.setAttribute('data-proxied', isProxied.toString());

		// Add "Public IP" default option
		let publicOption = document.createElement('option');
		publicOption.value = '_PUBLIC_';
		publicOption.text = 'Public IP';
		adapterSelect.appendChild(publicOption);

		// Add adapters for A/AAAA records
		if (isAorAAAA) {
			adapters?.forEach(adapter => {
				// Detect if adapter has private IP
				const isPrivate = adapter.ipAddress.includes(':')
					? isPrivateIPv6(adapter.ipAddress)
					: isPrivateIPv4(adapter.ipAddress);
				
				// Skip private adapters if the record is proxied (Cloudflare requires public IPs)
				if (isProxied && isPrivate) {
					return; // Don't add this adapter to the dropdown
				}

				let option = document.createElement('option');
				option.value = adapter.name;
				const ipLabel = isPrivate ? '🏠 Private' : '🌐 Public';
				option.text = `${adapter.name} (${adapter.ipAddress}) ${ipLabel}`;
				option.setAttribute('data-private', isPrivate.toString());
				adapterSelect.appendChild(option);
			});
		}

		// Set current selection
		// If previously selected adapter was private and record is now proxied, reset to Public IP
		let selectedAdapter = entry.networkAdapterName;
		if (isAorAAAA && selectedAdapter && selectedAdapter !== '_PUBLIC_') {
			// Check if the selected adapter is still in the dropdown
			const optionExists = Array.from(adapterSelect.options).some(opt => opt.value === selectedAdapter);
			if (optionExists) {
				adapterSelect.value = selectedAdapter;
			} else {
				// Adapter was filtered out (likely private IP on proxied record), reset to Public IP
				adapterSelect.value = '_PUBLIC_';
				console.warn(`Adapter "${selectedAdapter}" for "${entry.dnsName}" was reset to Public IP (likely private IP on proxied record)`);
			}
		} else {
			adapterSelect.value = '_PUBLIC_';
		}

		// Disable dropdown only for non A/AAAA types
		if (!isAorAAAA) {
			adapterSelect.disabled = true;
			adapterSelect.title = `${normalizedType} records must use a public IP address`;
			adapterSelect.style.cursor = 'not-allowed';
		} else if (isProxied) {
			adapterSelect.title = 'Proxied by Cloudflare - only public IP addresses shown';
		}
		
		adapterSelect.addEventListener('change', saveDnsUpdatable);
		row.insertCell().appendChild(adapterSelect);
		
		let ttlDisplay = entry.ttl! == 1 ? 'Auto' : (entry.ttl! / 60).toString();
		if (ttlDisplay != 'Auto')
			customTtl = true;
		row.insertCell().innerHTML = ttlDisplay;
		// Safely display proxied status (treat only true as proxied)
		row.insertCell().innerHTML = (entry.proxied === true).toString();
		tableBodyEl.appendChild(row);
	});

	// if all TTLs are auto, hide the units
	if (customTtl)
		(document.getElementById("ttlHeader") as HTMLElement)!.innerText = 'TTL (minutes)';

	// show table and some other elements
	spinnerEl.classList.add("hidden");
	dnsEntriesContainerEl.classList.remove("hidden");
	dnsUpdateEl.classList.remove("hidden");
}

async function saveDnsUpdatable() {
	let selectedDomainEls = document.querySelectorAll(".dns-entry-update:checked") as NodeListOf<HTMLInputElement>;
	let data: SelectedDomain[] = [];
	let validationErrors: string[] = [];
	
	selectedDomainEls.forEach((el) => {
		// Get corresponding adapter dropdown
		let index = el.name.replace('dns-entry-update', '');
		let adapterSelect = document.getElementById(`dns-entry-adapter${index}`) as HTMLSelectElement;
		
		let sd = new SelectedDomain();
		sd.zoneName = el.getAttribute("data-zone-name")!;
		sd.dnsName = el.getAttribute("data-dns-name")!;
		sd.type = el.getAttribute("data-dns-type")!;
		sd.networkAdapterName = adapterSelect?.value === '_PUBLIC_' ? null : adapterSelect?.value;
		
		// Validate: non-A/AAAA records must use public IP
		if (sd.type !== 'A' && sd.type !== 'AAAA' && sd.networkAdapterName) {
			validationErrors.push(`${sd.dnsName} (${sd.type}): This record type requires a public IP address`);
		}
		
		// Validate: proxied records with private adapters (shouldn't happen with current UI)
		const isProxied = adapterSelect?.getAttribute('data-proxied') === 'true';
		if (isProxied && sd.networkAdapterName) {
			// Check if selected adapter is private by looking at the selected option
			const selectedOption = adapterSelect.querySelector(`option[value="${sd.networkAdapterName}"]`);
			const isPrivate = selectedOption?.getAttribute('data-private') === 'true';
			if (isPrivate) {
				validationErrors.push(`${sd.dnsName}: Proxied records cannot use private IP addresses (Cloudflare limitation)`);
			}
		}
		
		data.push(sd);
	});
	
	// Show validation errors if any
	if (validationErrors.length > 0) {
		alert('Validation errors:\n\n' + validationErrors.join('\n'));
		return;
	}

	await saveDomainsAsync(data);
}

function setUiElementState() {
	if (_settings?.protocolSupport != 1)
		(document.getElementById("public-ipv4-info") as HTMLDivElement).classList.remove("hidden");
	if (_settings?.protocolSupport != 0)
		(document.getElementById("public-ipv6-info") as HTMLDivElement).classList.remove("hidden");
}

function setupSse() {
	let source = new EventSource('/sse');

	source.addEventListener('log-updated', function () {
		getLog();
	}, false);

	source.addEventListener('last-run', function (e) {
		let date = parseISO(e.data);
		(document.getElementById("lastUpdate")! as HTMLInputElement).value = format(date, "yyyy-MM-dd HH:mm:ss");
		document.getElementById('ipAddressInfoSpinner')?.classList.add("hidden");
		document.getElementById('ipAddressInfo')?.classList.remove("hidden");
	}, false);

	source.addEventListener('next-run', function (e) {
		let date = parseISO(e.data);
		(document.getElementById("nextUpdate")! as HTMLInputElement).value = format(date, "yyyy-MM-dd HH:mm:ss");
		document.getElementById('ipAddressInfoSpinner')?.classList.add("hidden");
		document.getElementById('ipAddressInfo')?.classList.remove("hidden");
	}, false);

	source.addEventListener('ipv4-address', function (e) {
		(document.getElementById("public-ipv4")! as HTMLInputElement).value = e.data;
		document.getElementById('ipAddressInfoSpinner')?.classList.add("hidden");
		document.getElementById('ipAddressInfo')?.classList.remove("hidden");
	}, false);

	source.addEventListener('ipv6-address', function (e) {
		(document.getElementById("public-ipv6")! as HTMLInputElement).value = e.data;
		document.getElementById('ipAddressInfoSpinner')?.classList.add("hidden");
		document.getElementById('ipAddressInfo')?.classList.remove("hidden");
	}, false);

	source.addEventListener('ip-address-changed', function () {
		getSelectedDnsEntries();
	}, false);
}
async function getRunInfo() {
	let runInfo = await getRunInfoAsync();

	(document.getElementById("lastUpdate")! as HTMLInputElement).value = format(parseISO(runInfo!.lastRun), "yyyy-MM-dd HH:mm:ss");
	(document.getElementById("nextUpdate")! as HTMLInputElement).value = format(parseISO(runInfo!.nextRun), "yyyy-MM-dd HH:mm:ss");
}

