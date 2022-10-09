using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;

namespace DnsTube
{
	public class Utility
	{
		public const string Ipv4Regex = @"\b(?:(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\b";

		public async static Task<Tuple<string, string>> GetPublicIpAddressAsync(IpSupport protocol, HttpClient Client)
		{
			string publicIpAddress = null;
			var maxAttempts = 3;
			var attempts = 0;
			string errorMesssage = null;

			var settings = new Settings();
			var url = protocol == IpSupport.IPv4 ? settings.IPv4_API : settings.IPv6_API;

			while (publicIpAddress == null && attempts < maxAttempts)
			{
				try
				{
					attempts++;
					var response = await Client.GetStringAsync(url);
					var candidatePublicIpAddress = response.Replace("\n", "");
					var regex = new Regex(Ipv4Regex);
					if (regex.IsMatch(candidatePublicIpAddress))
					{
						candidatePublicIpAddress = regex.Match(response).Value;
					}

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
			
			return new Tuple<string, string>(publicIpAddress, errorMesssage);
		}

		public async static Task<GithubRelease> GetLatestReleaseAsync()
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
					var response = await client.GetStringAsync(url);
					release = JsonSerializer.Deserialize<GithubRelease>(response);
				}
				catch { }
			}
			return release;
		}

		public static string GetDateString()
		{
			return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
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
