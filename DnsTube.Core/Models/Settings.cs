using System.Text.Json;

using DnsTube.Core.Enums;

namespace DnsTube.Core.Interfaces
{
	public class Settings : ISettings
	{
		public string EmailAddress { get; set; } = string.Empty;
		public bool IsUsingToken { get; set; }
		public string ApiKeyOrToken { get; set; } = string.Empty;
		public int UpdateIntervalMinutes { get; set; }
		public string PublicIpv4Address { get; set; } = string.Empty;
		public string PublicIpv6Address { get; set; } = string.Empty;
		public List<SelectedDomain> SelectedDomains { get; set; } = new List<SelectedDomain>();
		public bool SkipCheckForNewReleases { get; set; }
		public IpSupport ProtocolSupport { get; set; }
		public string? ZoneIDs { get; set; } = string.Empty;
		public string IPv4_API { get; set; } = string.Empty;
		public string IPv6_API { get; set; } = string.Empty;
		public string? NetworkAdapter { get; set; }

		public Settings()
		{
		}

		internal Settings(ISettingsDTO settingDTO)
		{
			ApiKeyOrToken = settingDTO.ApiKeyOrToken;
			EmailAddress = settingDTO.EmailAddress;
			IPv4_API = settingDTO.IPv4_API;
			IPv6_API = settingDTO.IPv6_API;
			IsUsingToken = settingDTO.IsUsingToken;
			NetworkAdapter = settingDTO.NetworkAdapter;
			ProtocolSupport = (IpSupport)settingDTO.ProtocolSupport;
			PublicIpv4Address = settingDTO.PublicIpv4Address;
			PublicIpv6Address = settingDTO.PublicIpv6Address;
			SelectedDomains = JsonSerializer.Deserialize<List<SelectedDomain>>(settingDTO.SelectedDomains) ?? new List<SelectedDomain>();
			SkipCheckForNewReleases = settingDTO.SkipCheckForNewReleases;
			UpdateIntervalMinutes = settingDTO.UpdateIntervalMinutes;
			ZoneIDs = settingDTO.ZoneIDs;
		}

		/// <summary>
		/// Make sure settings are populated
		/// </summary>
		/// <returns></returns>
		public bool Validate()
		{
			if (string.IsNullOrWhiteSpace(EmailAddress)
				|| !EmailAddress.Contains('@')
				|| string.IsNullOrWhiteSpace(ApiKeyOrToken)
				|| UpdateIntervalMinutes < 1
			)
				return false;

			return true;
		}

		public void Save()
		{
			throw new NotImplementedException();
		}
	}
}
