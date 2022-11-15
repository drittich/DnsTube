using DnsTube.Core.Enums;

namespace DnsTube.Core.Interfaces
{
	public interface ISettings
	{
		string ApiKeyOrToken { get; set; }
		string EmailAddress { get; set; }
		string IPv4_API { get; set; }
		string IPv6_API { get; set; }
		bool IsUsingToken { get; set; }
		IpSupport ProtocolSupport { get; set; }
		string PublicIpv4Address { get; set; }
		string PublicIpv6Address { get; set; }
		List<SelectedDomain> SelectedDomains { get; set; }
		bool SkipCheckForNewReleases { get; set; }
		int UpdateIntervalMinutes { get; set; }
		string? ZoneIDs { get; set; }
	}
}