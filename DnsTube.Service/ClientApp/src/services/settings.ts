import { NetworkAdapter } from "../model/NetworkAdapter";
import { RunInfo } from "../model/RunInfo";
import { SelectedDomain } from "../model/SelectedDomain";
import { Settings } from "../model/Settings";

export async function getSettingsAsync(): Promise<Settings | null> {
	let response = await fetch("/api/settings");
	if (response.ok) {
		let settings: Settings = await response.json();
		return settings;
	}
	else {
		console.log(response.statusText);
		return null;
	}
}

export async function saveSettingsAsync(settings: FormData): Promise<boolean> {
	try {
		await fetch("api/settings",
			{
				body: settings,
				method: "post"
			}
		);
		return true;
	} catch (error) {
		console.error('Error:', error);
		return false;
	}
}

export async function saveDomainsAsync(domains: SelectedDomain[]): Promise<boolean> {
	try {
		await fetch("api/settings/entries", {
			body: JSON.stringify(domains),
			method: "post",
			headers: {
				'Content-Type': 'application/json'
			}
		});
		return true;
	} catch (error) {
		console.error('Error:', error);
		return false;
	}
}

export async function getDbFolderAsync(): Promise<string | null> {
	let response = await fetch("/api/settings/dbpath");
	if (response.ok) {
		return await response.text();
	}
	else {
		console.log(response.statusText);
		return null;
	}
}

export async function getRunInfoAsync(): Promise<RunInfo | null> {
	let response = await fetch("/api/settings/runinfo");
	if (response.ok) {
		return await response.json() as RunInfo;
	}
	else {
		console.log(response.statusText);
		return null;
	}
}

export async function getNetworkAdapters(): Promise<NetworkAdapter[] | null> {
	let response = await fetch("/api/settings/adapters");
	if (response.ok) {
		return await response.json();
	}
	else {
		console.log(response.statusText);
		return null;
	}
}