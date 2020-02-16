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
				IsUsingToken = settings.IsUsingToken;
				ApiKey = settings.ApiKey;
				ApiToken = settings.ApiToken;
				UpdateIntervalMinutes = settings.UpdateIntervalMinutes;
				SelectedDomains = settings.SelectedDomains;
				StartMinimized = settings.StartMinimized;
			}
			else
			{
				UpdateIntervalMinutes = 30;
				SelectedDomains = new List<SelectedDomain>();
				IsUsingToken = true;
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
				string localApplicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string appDirectory = Path.Combine(localApplicationDataDirectory, "DnsTube");
				Directory.CreateDirectory(appDirectory);
				_getSettingsFilePath = Path.Combine(appDirectory, "config.json");
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
		public bool IsUsingToken { get; set; }
		public string ApiKey { get; set; }
		public string ApiToken { get; set; }
		public int UpdateIntervalMinutes { get; set; }
		public string PublicIpAddress { get; set; }
		public List<SelectedDomain> SelectedDomains { get; set; }
		public bool StartMinimized { get; set; }
	}
}
