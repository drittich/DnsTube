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

		public void DisplayVersionAndSettingsPath(Action<string> appendStatusText)
		{
	
			if (!Settings.SkipCheckForNewReleases)
			{
				var release = Utility.GetLatestRelease();
				if (release != null && release.tag_name != RELEASE_TAG)
					appendStatusText("You are not running the latest release. See https://github.com/drittich/DnsTube/releases/latest for more information.");
			}

			if (File.Exists(Utility.GetSettingsFilePath()))
				appendStatusText($"Settings path: {Utility.GetSettingsFilePath()}");
		}

		/// <summary>
		/// Returns true if an update was performed
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public bool UpdateDnsRecords(IpSupport protocol, string currentIpv4Address, string currentIpv6Address, Action<string> appendStatusText, Action<IpSupport, string> displayPublicIpAddress)
		{
			var publicIpAddress = GetPublicIpAddress(protocol, currentIpv4Address, currentIpv6Address, appendStatusText, displayPublicIpAddress);
			if (publicIpAddress == null)
			{
				appendStatusText($"Error detecting public {protocol} address");
				return false;
			}

			var oldPublicIpAddress = protocol == IpSupport.IPv4 ? Settings.PublicIpv4Address : Settings.PublicIpv6Address;

			if (publicIpAddress == oldPublicIpAddress)
				return false;

			if (protocol == IpSupport.IPv4)
				Settings.PublicIpv4Address = publicIpAddress;
			else
				Settings.PublicIpv6Address = publicIpAddress;
			Settings.Save();

			if (oldPublicIpAddress != null)
				appendStatusText($"Public {protocol} changed from {oldPublicIpAddress} to {publicIpAddress}");

			displayPublicIpAddress(protocol, publicIpAddress);

			var typesToUpdateForThisProtocol = new List<string> {
					"SPF",
					"TXT",
					protocol == IpSupport.IPv4 ? "A" : "AAAA"
				};

			// Get requested entries to update
			List<Dns.Result> potentialEntriesToUpdate = null;
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
				appendStatusText($"Error getting DNS records");
				appendStatusText(ex.Message);
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

					appendStatusText($"Updated {entry.type} record [{entry.name}] in zone [{entry.zone_name}] to {content}");
				}
				catch (Exception ex)
				{
					appendStatusText($"Error updating [{entry.type}] record [{entry.name}] in zone [{entry.zone_name}] to {content}");
					appendStatusText(ex.Message);
				}
			}
			//TODO: which of these is right? look at history
			return true;

			return false;
		}

		public string GetPublicIpAddress(IpSupport protocol, string currentIpv4Address, string currentIpv6Address, Action<string> appendStatusText, Action<IpSupport, string> displayPublicIpAddress)
		{
			string? errorMesssage;
			var publicIpAddress = Utility.GetPublicIpAddress(protocol, HttpClient, out errorMesssage);

			// Abort if we get an error, keeping the current address in settings
			if (publicIpAddress == null)
			{
				appendStatusText($"Error getting public {protocol}: {errorMesssage}");
				return null;
			}

			if ((protocol == IpSupport.IPv4 && currentIpv4Address != publicIpAddress) || (protocol == IpSupport.IPv6 && currentIpv6Address != publicIpAddress))
				displayPublicIpAddress(protocol, publicIpAddress);

			return publicIpAddress;
		}

		public void DisplayAndLogPublicIpAddress(Settings settings, string currentIpv4Address, string currentIpv6Address, Action<string> appendStatusText, Action<IpSupport, string> displayPublicIpAddress)
		{
			if (settings.ProtocolSupport != IpSupport.IPv6)
			{
				var publicIpv4Address = GetPublicIpAddress(IpSupport.IPv4, currentIpv4Address, currentIpv6Address, appendStatusText, displayPublicIpAddress);
				if (publicIpv4Address == null)
					appendStatusText($"Error detecting public IPv4 address");
				else
					appendStatusText($"Detected public IPv4 {publicIpv4Address}");
			}
			if (settings.ProtocolSupport != IpSupport.IPv4)
			{
				var publicIpv6Address = GetPublicIpAddress(IpSupport.IPv6, currentIpv4Address, currentIpv6Address, appendStatusText, displayPublicIpAddress);
				if (publicIpv6Address == null)
					appendStatusText($"Error detecting public IPv6 address");
				else
					appendStatusText($"Detected public IPv6 {publicIpv6Address}");
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
