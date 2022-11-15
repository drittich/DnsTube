using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DnsTube.Core.Interfaces;

namespace DnsTube.Core.Models
{
	internal class SettingsDTO : ISettingsDTO
	{
		public string ApiKeyOrToken { get; set; }
		public string EmailAddress { get; set; }
		public string IPv4_API { get; set; }
		public string IPv6_API { get; set; }
		public bool IsUsingToken { get; set; }
		public int ProtocolSupport { get; set; }
		public string PublicIpv4Address { get; set; }
		public string PublicIpv6Address { get; set; }
		public string SelectedDomains { get; set; }
		public bool SkipCheckForNewReleases { get; set; }
		public int UpdateIntervalMinutes { get; set; }
		public string ZoneIDs { get; set; }
	}
}
