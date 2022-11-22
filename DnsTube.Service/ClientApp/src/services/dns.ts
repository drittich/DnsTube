import { DnsEntry } from "../model/DnsEntry";

export async function getDnsEntriesAsync(): Promise<DnsEntry[] | null> {
	let response = await fetch("/api/dns");
	if (response.ok) {
		let dnsEntries: DnsEntry[] = await response.json();
		return dnsEntries;
	}
	else {
		console.log(response.statusText);
		return null;
	}
}

export async function updateDnsAsync() {
	let response = await fetch("/api/dns/update", {
		method: "POST"
	});
	if (!response.ok) {
		console.log(response.statusText);
		alert(response.statusText);
	}
}