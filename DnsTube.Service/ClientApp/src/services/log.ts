import { LogEntry } from "../model/LogEntry";

export async function getLogAsync(): Promise<LogEntry[]> {
	let logEntries = await fetch("/api/log")
		.then(response => response.json())
		.then((obj: LogEntry[]) => {
			return obj;
		});

	return logEntries;
}