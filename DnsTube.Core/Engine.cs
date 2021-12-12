using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DnsTube.Core
{
	public class Engine
	{
		public HttpClient HttpClient { get; set; }
		public CloudflareAPI CloudflareAPI { get; set; }
		public Settings Settings { get; set; }
		public string RELEASE_TAG = "v0.9.4";


		public Engine(Settings settings)
		{
			Settings = settings;

			HttpClient = new HttpClient();
			// use TLS 1.2
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

			InitCloudflareClient(Settings);
		}

		public void InitCloudflareClient(Settings settings)
		{
			CloudflareAPI = new CloudflareAPI(HttpClient, settings);
		}

		public void DisplayNewReleasePrompt(Action<string> appendStatusText)
		{
	
			if (!Settings.SkipCheckForNewReleases)
			{
				var release = Utility.GetLatestRelease();
				if (release != null && release.tag_name != RELEASE_TAG)
					appendStatusText("You are not running the latest release. See https://github.com/drittich/DnsTube/releases/latest for more information.");
			}

		}

		/// <summary>
		/// Returns true if an update was performed
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public bool UpdateDnsRecords(IpSupport protocol, string publicIpAddress, out List<string> messages)
		{
			bool updateWasDone = false;
			messages = new List<string>();

			var typesToUpdateForThisProtocol = new List<string> {
					"SPF",
					"TXT",
					protocol == IpSupport.IPv4 ? "A" : "AAAA"
				};

			// Get requested entries to update
			List<Dns.Result>? potentialEntriesToUpdate = null;
			try
			{
				var allRecordsByZone = CloudflareAPI.GetAllDnsRecordsByZone();

				potentialEntriesToUpdate = allRecordsByZone.Where(d => Settings.SelectedDomains.Any(s =>
					s.ZoneName == d.zone_name
					&& s.DnsName == d.name
					&& s.Type == d.type)
					&& typesToUpdateForThisProtocol.Contains(d.type)).ToList();
			}
			catch (Exception ex)
			{
				messages.Add($"Error getting DNS records");
				messages.Add(ex.Message);
			}

			// TODO:determine which ones need updating
			if (potentialEntriesToUpdate == null || !potentialEntriesToUpdate.Any())
				return false;

			foreach (var entry in potentialEntriesToUpdate)
			{
				string content;
				if (entry.type == "SPF" || entry.type == "TXT")
					content = UpdateDnsRecordContent(protocol, entry.content, publicIpAddress);
				else
					content = publicIpAddress;

				if (entry.content == content)
					continue;

				try
				{
					CloudflareAPI.UpdateDns(protocol, entry.zone_id, entry.id, entry.type, entry.name, content, entry.ttl, entry.proxied);
					updateWasDone = true;
					messages.Add($"Updated {entry.type} record [{entry.name}] in zone [{entry.zone_name}] to {content}");
				}
				catch (Exception ex)
				{
					messages.Add($"Error updating [{entry.type}] record [{entry.name}] in zone [{entry.zone_name}] to {content}");
					messages.Add(ex.Message);
				}
			}

			return updateWasDone;
		}

		public string? GetPublicIpAddress(IpSupport protocol, out string? errorMesssage)
		{
			string? publicIpAddress = null;
			var maxAttempts = 3;
			var attempts = 0;
			errorMesssage = null;

			var url = protocol == IpSupport.IPv4 ? Settings.IPv4_API : Settings.IPv6_API;

			while (publicIpAddress == null && attempts < maxAttempts)
			{
				try
				{
					attempts++;
					var response = HttpClient.GetStringAsync(url).Result;
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
			if (string.IsNullOrWhiteSpace(ipString))
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

		private string UpdateDnsRecordContent(IpSupport protocol, string content, string publicIpAddress)
		{
			// we need the explicit protocol for this method
			if (protocol == IpSupport.IPv4AndIPv6)
				throw new ArgumentOutOfRangeException();

			var newContent = content;

			if (protocol == IpSupport.IPv4)
				newContent = Regex.Replace(newContent, ipv4Regex, publicIpAddress);
			else if (protocol == IpSupport.IPv6)
				newContent = Regex.Replace(newContent, ipv6Regex, publicIpAddress);

			return newContent;
		}

		private const string ipv4Regex = @"\b(?:(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\b";
		private const string ipv6Regex = @"\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%.+)?\s*";

	}
}
