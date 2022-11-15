using System.Text.Json;

using DnsTube.Core.Enums;

namespace DnsTube.Core.Interfaces
{
	public class Settings : ISettings
	{
		public string EmailAddress { get; set; } = "";
		public bool IsUsingToken { get; set; }
		public string ApiKeyOrToken { get; set; } = "";
		public int UpdateIntervalMinutes { get; set; }
		public string PublicIpv4Address { get; set; } = "";
		public string PublicIpv6Address { get; set; } = "";
		public List<SelectedDomain> SelectedDomains { get; set; } = new List<SelectedDomain>();
		public bool SkipCheckForNewReleases { get; set; }
		public IpSupport ProtocolSupport { get; set; }
		public string? ZoneIDs { get; set; } = "";
		public string IPv4_API { get; set; } = "";
		public string IPv6_API { get; set; } = "";

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
			ProtocolSupport = (IpSupport)settingDTO.ProtocolSupport;
			PublicIpv4Address = settingDTO.PublicIpv4Address;
			PublicIpv6Address = settingDTO.PublicIpv6Address;
			SelectedDomains = JsonSerializer.Deserialize<List<SelectedDomain>>(settingDTO.SelectedDomains) ?? new List<SelectedDomain>();
			SkipCheckForNewReleases = settingDTO.SkipCheckForNewReleases;
			UpdateIntervalMinutes = settingDTO.UpdateIntervalMinutes;
			ZoneIDs = settingDTO.ZoneIDs;
		}

		//public void LoadFromConfigFile(string configPath)
		//{
		//	ConfigPath = configPath;

		//	if (!File.Exists(ConfigPath))
		//	{
		//		UpdateIntervalMinutes = 30;
		//		SelectedDomains = new List<SelectedDomain>();
		//		IsUsingToken = true;
		//		ProtocolSupport = IpSupport.IPv4;
		//		ZoneIDs = "";
		//		IPv4_API = "https://api.ipify.org/";
		//		IPv6_API = "https://api64.ipify.org/";

		//		Save();

		//		return;
		//	}

		//	string json = File.ReadAllText(ConfigPath);
		//	var settings = JsonSerializer.Deserialize<Settings>(json);

		//	if (settings == null)
		//		throw new Exception($"Unable to parse {ConfigPath}");

		//	EmailAddress = settings.EmailAddress;
		//	IsUsingToken = settings.IsUsingToken;
		//	ApiKey = settings.ApiKey;
		//	ApiToken = settings.ApiToken;
		//	UpdateIntervalMinutes = settings.UpdateIntervalMinutes;
		//	SelectedDomains = settings.SelectedDomains;
		//	ProtocolSupport = settings.ProtocolSupport;
		//	SkipCheckForNewReleases = settings.SkipCheckForNewReleases;
		//	ZoneIDs = settings.ZoneIDs ?? "";
		//	IPv4_API = settings.IPv4_API ?? "https://api.ipify.org/";
		//	IPv6_API = settings.IPv6_API ?? "https://api64.ipify.org/";
		//	StartMinimized = settings.StartMinimized;
		//	MinimizeToTray = settings.MinimizeToTray;
		//}

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
