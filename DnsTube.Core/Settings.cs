using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using DnsTube.Core;

namespace DnsTube
{
	public class Settings : SettingsDTO
	{
		public Settings()
		{
			if (File.Exists(Utility.GetSettingsFilePath()))
			{
				string json = File.ReadAllText(Utility.GetSettingsFilePath());
				var settings = JsonSerializer.Deserialize<SettingsDTO>(json);
				EmailAddress = settings.EmailAddress;
				IsUsingToken = settings.IsUsingToken;
				ApiKey = settings.ApiKey;
				ApiToken = settings.ApiToken;
				UpdateIntervalMinutes = settings.UpdateIntervalMinutes;
				SelectedDomains = settings.SelectedDomains;
				StartMinimized = settings.StartMinimized;
				MinimizeToTray = settings.MinimizeToTray;
				ProtocolSupport = settings.ProtocolSupport;
				SkipCheckForNewReleases = settings.SkipCheckForNewReleases;
				ZoneIDs = settings.ZoneIDs ?? "";
				IPv4_API = settings.IPv4_API ?? "https://api.ipify.org/";
				IPv6_API = settings.IPv6_API ?? "https://api64.ipify.org/";
			}
			else
			{
				UpdateIntervalMinutes = 30;
				SelectedDomains = new List<SelectedDomain>();
				IsUsingToken = true;
				ProtocolSupport = IpSupport.IPv4;
				ZoneIDs = "";
				IPv4_API = "https://api.ipify.org/";
				IPv6_API = "https://api64.ipify.org/";
			}
		}

		/// <summary>
		/// Make sure settings are populated
		/// </summary>
		/// <returns></returns>
		public bool Validate()
		{
			if (string.IsNullOrWhiteSpace(EmailAddress)
				|| !EmailAddress.Contains("@")
				|| (!IsUsingToken && string.IsNullOrWhiteSpace(ApiKey))
				|| (IsUsingToken && string.IsNullOrWhiteSpace(ApiToken))
				|| UpdateIntervalMinutes == 0
			)
				return false;

			return true;
		}

		public void Save()
		{
			string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(Core.Utility.GetSettingsFilePath(), json);
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
		public bool MinimizeToTray { get; set; }
		public bool SkipCheckForNewReleases { get; set; }
		public IpSupport ProtocolSupport { get; set; }
		public string ZoneIDs { get; set; }

		public string IPv4_API { get; set; }

		public string IPv6_API { get; set; }
	}
}
