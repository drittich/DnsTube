export class DnsEntry {
	public updateCloudflare: boolean | undefined;
	public dnsName: string | undefined;
	public type: string | undefined;
	public address: string | undefined;
	public ttl: number | undefined;
	public proxied: boolean | undefined;
	public zoneName: string | undefined;
}