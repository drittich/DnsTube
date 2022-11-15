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
	return fetch("api/settings",
		{
			body: settings,
			method: "post"
		})
		.then(() => {
			return true;
		})
		.catch(error => {
			console.error('Error:', error);
			return false;
		});
}

export async function saveDomainsAsync(domains: SelectedDomain[]): Promise<boolean> {
	return fetch("api/settings/entries",
		{
			body: JSON.stringify(domains),
			method: "post",
			headers: {
				'Content-Type': 'application/json'
			}
		})
		.then(() => {
			return true;
		})
		.catch(error => {
			console.error('Error:', error);
			return false;
		});
}

export async function getDbFolderAsync(): Promise<string | null> {
	let response = await fetch("/api/settings/dbfolder");
	if (response.ok) {
		return await response.text();
	}
	else {
		console.log(response.statusText);
		return null;
	}
}