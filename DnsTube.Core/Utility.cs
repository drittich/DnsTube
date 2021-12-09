﻿using System.Text.Json;
using System.Text.RegularExpressions;

namespace DnsTube.Core
{
	public class Utility
	{
		public static string GetSettingsFilePath()
		{
#if PORTABLE
				// save config in application directory
				var appDirectory = Directory.GetCurrentDirectory();
#elif SERVICE
				// save config in application directory
				var appDirectory = Directory.GetCurrentDirectory();
#else
			// save config under user profile
			string localApplicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string appDirectory = Path.Combine(localApplicationDataDirectory, "DnsTube");
			Directory.CreateDirectory(appDirectory);
#endif
			return Path.Combine(appDirectory, "config.json");
		}

		public static string GetPublicIpAddress(IpSupport protocol, HttpClient Client, out string errorMesssage)
		{
			string publicIpAddress = null;
			var maxAttempts = 3;
			var attempts = 0;
			errorMesssage = null;

			var settings = new Settings();
			var url = protocol == IpSupport.IPv4 ? settings.IPv4_API : settings.IPv6_API;

			while (publicIpAddress == null && attempts < maxAttempts)
			{
				try
				{
					attempts++;
					var response = Client.GetStringAsync(url).Result;
					var candidatePublicIpAddress = response.Replace("\n", "");

					if (!IsValidIpAddress(protocol, candidatePublicIpAddress))
						throw new Exception($"Malformed response, expected IP address: {response}");

					publicIpAddress = candidatePublicIpAddress;
				}
				catch (Exception e)
				{
					if (attempts >= maxAttempts)
						errorMesssage = e.Message;
				}
			}
			return publicIpAddress;
		}

		public static bool IsValidIpAddress(IpSupport protocol, string ipString)
		{
			if (String.IsNullOrWhiteSpace(ipString))
				return false;

			if (protocol == IpSupport.IPv4)
			{
				string[] splitValues = ipString.Split('.');
				if (splitValues.Length != 4)
					return false;

				byte tempForParsing;
				return splitValues.All(r => byte.TryParse(r, out tempForParsing));
			}
			else
			{
				var regex = new Regex(@"(?:^|(?<=\s))(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))(?=\s|$)");
				var match = regex.Match(ipString);
				return match.Success;
			}
		}

		public static string GetDateString()
		{
			return DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");
		}

		public static GithubRelease GetLatestRelease()
		{
			var url = "https://api.github.com/repos/drittich/DnsTube/releases/latest";

			GithubRelease release = null;
			using (var client = new HttpClient())
			{
				client.Timeout = TimeSpan.FromSeconds(15);

				try
				{
					client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
					client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
					var response = client.GetStringAsync(url).Result;
					release = JsonSerializer.Deserialize<GithubRelease>(response);
				}
				catch { }
			}
			return release;
		}
	}
}
