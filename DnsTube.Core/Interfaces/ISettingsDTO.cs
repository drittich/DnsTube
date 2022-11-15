using DnsTube.Core.Enums;

namespace DnsTube.Core.Interfaces
{
	public interface ISettingsDTO
	{
		string ApiKeyOrToken { get; set; }
		string EmailAddress { get; set; }
		string IPv4_API { get; set; }
		string IPv6_API { get; set; }
		bool IsUsingToken { get; set; }
		int ProtocolSupport { get; set; }
		string PublicIpv4Address { get; set; }
		string PublicIpv6Address { get; set; }
		string SelectedDomains { get; set; }
		bool SkipCheckForNewReleases { get; set; }
		int UpdateIntervalMinutes { get; set; }
		string ZoneIDs { get; set; }
	}
}