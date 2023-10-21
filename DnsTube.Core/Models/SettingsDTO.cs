using DnsTube.Core.Interfaces;

namespace DnsTube.Core.Models
{
	internal class SettingsDTO : ISettingsDTO
	{
		public required string ApiKeyOrToken { get; set; }
		public required string EmailAddress { get; set; }
		public required string IPv4_API { get; set; }
		public required string IPv6_API { get; set; }
		public bool IsUsingToken { get; set; }
		public int ProtocolSupport { get; set; }
		public required string PublicIpv4Address { get; set; }
		public required string PublicIpv6Address { get; set; }
		public required string SelectedDomains { get; set; }
		public bool SkipCheckForNewReleases { get; set; }
		public int UpdateIntervalMinutes { get; set; }
		public required string ZoneIDs { get; set; }
		public required string NetworkAdapter { get; set; }
	}
}
