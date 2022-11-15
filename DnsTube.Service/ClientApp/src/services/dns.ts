import { DnsEntry } from "../model/DnsEntry";

export async function getDnsEntries(): Promise<DnsEntry[] | null> {
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
