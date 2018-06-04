using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DnsTube
{
	public class Settings : SettingsDTO
	{
		public Settings()
		{
			if (File.Exists(GetSettingsFilePath()))
			{
				string json = File.ReadAllText(GetSettingsFilePath());
				var settings = JsonConvert.DeserializeObject<SettingsDTO>(json);
				EmailAddress = settings.EmailAddress;
				ApiKey = settings.ApiKey;
				UpdateIntervalMinutes = settings.UpdateIntervalMinutes;
				SelectedDomains = settings.SelectedDomains;
			}
			else
			{
				UpdateIntervalMinutes = 30;
				SelectedDomains = new List<SelectedDomain>();
			}
		}

		public void Save()
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(GetSettingsFilePath(), json);
		}

		string _getSettingsFilePath;
		public string GetSettingsFilePath()
		{
			if (_getSettingsFilePath == null)
			{
				string localApplicationDataFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				_getSettingsFilePath = Path.Combine(localApplicationDataFilePath, "DnsTube", "config.json");
			}
			return _getSettingsFilePath;
		}
	}

	public class SelectedDomain
	{
		public string ZoneName { get; set; }
		public string DnsName { get; set; }
	}

	public class SettingsDTO
	{
		public string EmailAddress { get; set; }
		public string ApiKey { get; set; }
		public int UpdateIntervalMinutes { get; set; }
		public string PublicIpAddress { get; set; }
		public List<SelectedDomain> SelectedDomains { get; set; }
	}
}
