using System;
using System.Linq;
using System.Collections.Generic;

using DnsTube.Core;
using DnsTube.Core.Dns;

namespace DnsTube.Console
{
	internal class Program
	{
		static Settings settings;
		static Engine engine;

		static void Main(string? configPath = null)
		{
			configPath = configPath ?? Utility.GetSettingsFilePath();
			AppendStatusText($"Looking for config in {configPath}");

			settings = new Settings(configPath);
			engine = new Engine(settings);
			var Title = $"DnsTube {engine.RELEASE_TAG}";
			AppendStatusText(Title);
			engine.DisplayNewReleasePrompt(AppendStatusText);
			PromptForSettings();
			FetchDsnEntries();
			DisplayAndLogPublicIpAddress();
			if (settings.Validate())
				ValidateSelectedDomains();
			DoUpdate();
		}

		private static void ValidateSelectedDomains()
		{
			// handle old settings where we did not save Type, or nothing selected at all
			if (!settings.SelectedDomains.Any(entry => entry.Type != null))
			{
				AppendStatusText("Please select the entries that you would like to update");

				// remove invalid entries
				settings.SelectedDomains.RemoveAll(entry => entry.Type == null);
			}
		}

		private static void FetchDsnEntries()
		{
			if (!PreflightSettingsCheck())
				return;

			try
			{
				var allDnsRecordsByZone = engine.CloudflareAPI.GetAllDnsRecordsByZone();
				LogDnsEntriesToUpdate(allDnsRecordsByZone);
			}
			catch (Exception e)
			{
				AppendStatusText($"Error fetching list: {e.Message}");
				if (settings.IsUsingToken && e.Message.Contains("403 (Forbidden)"))
					AppendStatusText($"Make sure your token has Zone:Read permissions. See https://dash.cloudflare.com/profile/api-tokens to configure.");
			}
		}

		private static void LogDnsEntriesToUpdate(List<Result> allDnsRecordsByZone)
		{
			AppendStatusText("Entries to update:");
			foreach (Result rec in allDnsRecordsByZone
				.Where(rec => settings.SelectedDomains.Any(d => d.ZoneName == rec.zone_name && d.DnsName == rec.name && d.Type == rec.type)))
			{
				var text = $"  Type:{rec.type} Zone:{rec.zone_name} Name:{rec.name} Address:{rec.content} TTL:{rec.ttl} Proxied:{rec.proxied}";
				AppendStatusText(text);
			}
		}

		private static void AppendStatusText(string s)
		{
			System.Console.WriteLine(s);
		}

		private static void DisplayPublicIpAddressThreadSafe(IpSupport protocol, string s)
		{
			//AppendStatusText($"IP Address: {s}");
		}

		private static void PromptForSettings()
		{
			if (!settings.Validate())
				AppendStatusText($"Please configure your settings.");
		}

		private static bool PreflightSettingsCheck()
		{
			if (!settings.Validate())
			{
				AppendStatusText($"Settings not configured");
				return false;
			}
			return true;
		}

		public static void DisplayAndLogPublicIpAddress()
		{
			string? errorMesssage;
			if (settings.ProtocolSupport != IpSupport.IPv6)
			{
				var publicIpv4Address = engine.GetPublicIpAddress(IpSupport.IPv4, out errorMesssage);
				if (publicIpv4Address == null)
				{
					AppendStatusText($"Error getting public IPv4 address: {errorMesssage}");
				}
				else
				{
					AppendStatusText($"Detected public IPv4 address {publicIpv4Address}");
				}

			}
			if (settings.ProtocolSupport != IpSupport.IPv4)
			{
				var publicIpv6Address = engine.GetPublicIpAddress(IpSupport.IPv6, out errorMesssage);
				if (publicIpv6Address == null)
				{
					AppendStatusText($"Error detecting public IPv6 address: {errorMesssage}");
				}
				else
				{
					AppendStatusText($"Detected public IPv6 address {publicIpv6Address}");
				}
			}
		}

		private static void DoUpdate()
		{
			if (!PreflightSettingsCheck())
				return;

			var addressWasUpdated = false;
			// if IPv6-only support was not specified, do the IPv4 update
			if (settings.ProtocolSupport != IpSupport.IPv6)
				if (UpdateDnsRecords(IpSupport.IPv4))
					addressWasUpdated = true;

			// if IPv4-only support was not specified, do the IPv6 update
			if (settings.ProtocolSupport != IpSupport.IPv4)
				if (UpdateDnsRecords(IpSupport.IPv6))
					addressWasUpdated = true;
			
			if (!addressWasUpdated)
				AppendStatusText("No update needed");
		}

		/// <summary>
		/// Returns true if an update was performed
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public static bool UpdateDnsRecords(IpSupport protocol)
		{
			string? errorMesssage;
			var publicIpAddress = engine.GetPublicIpAddress(protocol, out errorMesssage);
			if (publicIpAddress == null)
			{
				AppendStatusText($"Error getting public {protocol} address: {errorMesssage}");
			}
			//else
			//{
			//	AppendStatusText($"Detected public {protocol} address {publicIpAddress}");
			//}

			var oldPublicIpAddress = protocol == IpSupport.IPv4 ? settings.PublicIpv4Address : settings.PublicIpv6Address;

			if (publicIpAddress == oldPublicIpAddress)
				return false;

			if (protocol == IpSupport.IPv4)
				settings.PublicIpv4Address = publicIpAddress;
			else
				settings.PublicIpv6Address = publicIpAddress;
			settings.Save();

			if (oldPublicIpAddress != null)
				AppendStatusText($"Public {protocol} changed from {oldPublicIpAddress} to {publicIpAddress}");

			List<string> messages;
			var ret = engine.UpdateDnsRecords(protocol, publicIpAddress, out messages);
			foreach (var message in messages)
				AppendStatusText(message);

			return ret;
		}
	}
}
