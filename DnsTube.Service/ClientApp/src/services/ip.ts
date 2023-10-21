import { Ip } from "../model/Ip";

export async function getIp(): Promise<Ip | null> {
	try {
		const response = await fetch("/api/ip", { method: "get" });
		const obj: Ip = await response.json();
		return obj;
	} catch (error) {
		console.error('Error:', error);
		return null;
	}
}
