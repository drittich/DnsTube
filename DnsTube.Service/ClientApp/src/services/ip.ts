import { Ip } from "../model/Ip";

export async function getIp(): Promise<Ip | null> {
	return fetch("/api/ip",
		{
			method: "get"
		})
		.then(response => response.json())
		.then((obj: Ip) => {
			// console.log('ip in model', obj);
			return obj;
		})
		.catch(error => {
			console.error('Error:', error);
			return null;
		});
}
