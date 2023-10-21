import { LogEntry } from "../model/LogEntry";

export async function getLogAsync(pageSize?: number, lastId?: number): Promise<LogEntry[]> {
	let url = "/api/history";
	let params = new URLSearchParams();
	if (pageSize != null) {
		params.append("pageSize", pageSize.toString());
	}
	if (lastId != null) {
		params.append("lastId", lastId.toString());
	}
	if (params.toString() != "") {
		url += "?" + params.toString();
	}

	let response = await fetch(url);
	let logEntries: LogEntry[] = await response.json();

	return logEntries;
}


export async function deleteLogAsync(): Promise<string> {
	let response = await fetch("api/history",
		{
			method: "delete"
		});
	if (response.ok) {
		return "ok";
	}
	else {
		console.log(response.statusText);
		return response.statusText;
	}
}