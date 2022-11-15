import { SelectedDomain } from "./SelectedDomain";

export class Settings 
{
	public apiKeyOrToken: string | undefined;
	public emailAddress: string | undefined;
	public iPv4_API: string | undefined;
	public iPv6_API: string | undefined;
	public isUsingToken: boolean | undefined;
	public protocolSupport: number | undefined;
	public publicIpv4Address: string | undefined;
	public publicIpv6Address: string | undefined;
	public selectedDomains: SelectedDomain[] | undefined;
	public skipCheckForNewReleases:boolean | undefined;
	public updateIntervalMinutes: number | undefined;
	public zoneIDs: string | undefined;
}
