using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DnsTube
{
	public class Settings : SettingsDTO
	{
		public Settings()
		{
			if (File.Exists(GetSettingsFilePath()))
			{
				string json = File.ReadAllText(GetSettingsFilePath());
				var settings = JsonSerializer.Deserialize<SettingsDTO>(json);
				EmailAddress = settings.EmailAddress;
				IsUsingToken = settings.IsUsingToken;
				ApiKey = settings.ApiKey;
				ApiToken = settings.ApiToken;
				UpdateIntervalMinutes = settings.UpdateIntervalMinutes;
				SelectedDomains = settings.SelectedDomains;
				StartMinimized = settings.StartMinimized;
				ProtocolSupport = settings.ProtocolSupport;
				SkipCheckForNewReleases = settings.SkipCheckForNewReleases;
				ZoneIDs = settings.ZoneIDs ?? "";
				IPv4_API = settings.IPv4_API ?? "http://api.ipify.org/";
				IPv6_API = settings.IPv6_API ?? "https://api.ipify.org/";
			}
			else
			{
				UpdateIntervalMinutes = 30;
				SelectedDomains = new List<SelectedDomain>();
				IsUsingToken = true;
				ProtocolSupport = IpSupport.IPv4;
				ZoneIDs = "";
				IPv4_API = "http://api.ipify.org/";
				IPv6_API = "https://api.ipify.org/";
			}
		}

		public void Save()
		{
			string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(GetSettingsFilePath(), json);
		}

		private string _getSettingsFilePath;

		public string GetSettingsFilePath()
		{
			if (_getSettingsFilePath == null)
			{
#if PORTABLE
				// save config in application directory
				var appDirectory = Directory.GetCurrentDirectory();
#else
				// save config under user profile
				string localApplicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string appDirectory = Path.Combine(localApplicationDataDirectory, "DnsTube");
				Directory.CreateDirectory(appDirectory);
#endif
				_getSettingsFilePath = Path.Combine(appDirectory, "config.json");
			}
			return _getSettingsFilePath;
		}
	}

	public class SelectedDomain
	{
		public string ZoneName { get; set; }
		public string DnsName { get; set; }
		public string Type { get; set; }
	}

	public class SettingsDTO
	{
		public string EmailAddress { get; set; }
		public bool IsUsingToken { get; set; }
		public string ApiKey { get; set; }
		public string ApiToken { get; set; }
		public int UpdateIntervalMinutes { get; set; }
		public string PublicIpv4Address { get; set; }
		public string PublicIpv6Address { get; set; }
		public List<SelectedDomain> SelectedDomains { get; set; }
		public bool StartMinimized { get; set; }
		public bool SkipCheckForNewReleases { get; set; }
		public IpSupport ProtocolSupport { get; set; }
		public string ZoneIDs { get; set; }

		public string IPv4_API { get; set; }

		public string IPv6_API { get; set; }
	}

	public enum IpSupport
	{
		IPv4, IPv6, IPv4AndIPv6
	}
}
