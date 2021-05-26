using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DnsTube2
{
	public class Utility
	{
		public static string GetPublicIpAddress(IpSupport protocol, HttpClient Client, out string errorMesssage)
		{
			string publicIpAddress = null;
			var maxAttempts = 3;
			var attempts = 0;
			errorMesssage = null;
			var url = protocol == IpSupport.IPv4 ? "http://ipv4bot.whatismyipaddress.com" : "http://ipv6bot.whatismyipaddress.com";

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

		public static GithubRelease GetLatestRelease()
		{
			var url = "https://api.github.com/repos/drittich/DnsTube/releases/latest";

			GithubRelease release = null;
			using (var client = new HttpClient())
				try
				{
					client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
					client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
					var response = client.GetStringAsync(url).Result;
					release = JsonSerializer.Deserialize<GithubRelease>(response);
				}
				catch { }
			return release;
		}

		public static string GetDateString()
		{
			return DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");
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
	}

}
