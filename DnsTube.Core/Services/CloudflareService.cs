using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;
using DnsTube.Core.Models.Dns;

using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Services
{
	public class CloudflareService : ICloudflareService
	{
		private readonly ILogger<CloudflareService> _logger;
		private readonly ISettingsService _settingsService;
		private readonly ILogService _logService;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IIpAddressService _ipAddressService;

		public CloudflareService(ILogger<CloudflareService> logger, ISettingsService settingsService, ILogService logService, IHttpClientFactory httpClientFactory, IIpAddressService ipAddressService)
		{
			_logger = logger;
			_settingsService = settingsService;
			_logService = logService;
			_httpClientFactory = httpClientFactory;
			_ipAddressService = ipAddressService;
		}

		/// <summary>
		/// Returns true if an update was performed
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public async Task<bool> UpdateDnsRecordsAsync(IpSupport protocol, string publicIpAddress)
		{
			bool updateWasDone = false;

			var typesToUpdateForThisProtocol = new List<string> {
					"SPF",
					"TXT",
					protocol == IpSupport.IPv4 ? "A" : "AAAA"
				};

			// Get requested entries to update
			List<Result>? potentialEntriesToUpdate = null;
			var settings = await _settingsService.GetAsync();
			try
			{
				var allRecordsByZone = await GetAllDnsRecordsByZoneAsync();

				potentialEntriesToUpdate = allRecordsByZone.Where(d => settings.SelectedDomains.Any(s =>
					s.ZoneName == d.zone_name
					&& s.DnsName == d.name
					&& s.Type == d.type)
					&& typesToUpdateForThisProtocol.Contains(d.type)).ToList();
			}
			catch (Exception ex)
			{
				await _logService.WriteAsync("Error getting DNS records", LogLevel.Error);
				await _logService.WriteAsync(ex.Message, LogLevel.Error);
			}

			// TODO:determine which specific ones need updating
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
					await UpdateDnsAsync(protocol, entry.zone_id, entry.id, entry.type, entry.name, content, entry.ttl, entry.proxied);
					updateWasDone = true;
					await _logService.WriteAsync($"Updated {entry.type} record [{entry.name}] in zone [{entry.zone_name}] to {content}", LogLevel.Information);
				}
				catch (Exception ex)
				{
					await _logService.WriteAsync($"Error updating [{entry.type}] record [{entry.name}] in zone [{entry.zone_name}] to {content}", LogLevel.Error);
					await _logService.WriteAsync(ex.Message, LogLevel.Error);
				}
			}

			return updateWasDone;
		}

		private string UpdateDnsRecordContent(IpSupport protocol, string content, string publicIpAddress)
		{
			// we need the explicit protocol for this method
			if (protocol == IpSupport.IPv4AndIPv6)
				throw new ArgumentOutOfRangeException();

			var newContent = content;

			if (protocol == IpSupport.IPv4)
				newContent = Regex.Replace(newContent, Application.Ipv4RegexSubstring, publicIpAddress);
			else if (protocol == IpSupport.IPv6)
				newContent = Regex.Replace(newContent, Application.Ipv6RegexSubstring, publicIpAddress);

			return newContent;
		}

		/// <summary>
		/// Returns a list of zone IDs
		/// Ref: https://api.cloudflare.com/#zone-list-zones
		/// </summary>
		/// <returns></returns>
		public async Task<List<string>> ListZoneIDsAsync()
		{
			List<string> ret = new List<string>();
			int pageSize = 50;
			int pageNumber = 1;
			int totalPages;

			var httpClient = _httpClientFactory.CreateClient(HttpClientName.Cloudflare.ToString());
			do
			{
				var req = await GetRequestMessageAsync(HttpMethod.Get, $"zones?status=active&page={pageNumber}&per_page={pageSize}&order=name&direction=asc&match=all");

				var response = await httpClient.SendAsync(req);
				var result = await response.Content.ReadAsStringAsync();

				await ValidateCloudflareResultAsync(response, result, "list zones");

				var zoneListResponse = JsonSerializer.Deserialize<Models.Zone.ListZonesResponse>(result);

				int totalRecords = zoneListResponse.result_info.total_count;
				totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

				ret.AddRange(zoneListResponse.result.Select(z => z.id));

				pageNumber++;
			} while (pageNumber <= totalPages);

			return ret;
		}

		// Ref: https://api.cloudflare.com/#dns-records-for-a-zone-list-dns-records
		private async Task<List<Result>> GetRecordsByTypeAsync(string zoneIdentifier, string recordType)
		{
			int pageSize = 100;
			int pageNumber = 1;
			int totalPages;

			var ret = new List<Result>();

			var httpClient = _httpClientFactory.CreateClient(HttpClientName.Cloudflare.ToString());
			do
			{
				var req = await GetRequestMessageAsync(HttpMethod.Get, $"zones/{zoneIdentifier}/dns_records?type={recordType}&page={pageNumber}&per_page={pageSize}&order=name&direction=asc&match=all");

				var response = await httpClient.SendAsync(req);
				var result = await response.Content.ReadAsStringAsync();

				await ValidateCloudflareResultAsync(response, result, "list DNS records");

				var dnsRecordsResponse = JsonSerializer.Deserialize<DnsRecordsResponse>(result);

				int totalRecords = dnsRecordsResponse.result_info.total_count;
				totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

				ret.AddRange(dnsRecordsResponse.result);

				pageNumber++;
			} while (pageNumber <= totalPages);

			return ret;
		}

		private async Task ValidateCloudflareResultAsync(HttpResponseMessage response, string result, string action)
		{
			var settings = await _settingsService.GetAsync();
			if (!response.IsSuccessStatusCode)
			{
				var cfError = JsonSerializer.Deserialize<CloudflareApiError>(result);
				var cfMsg = cfError?.errors?.First().message;
				await _logService.WriteAsync($"Cloudflare API error: {cfMsg}", LogLevel.Error);
				if (settings.IsUsingToken)
				{

					throw new Exception($"Unable to {action}. If you are updating all zones, token permissions should be similar to [All zones - Zone:Read, DNS:Edit]. If your token only has permissions for specific zones, click Settings and configure the Zone IDs with a comma-separated list.\r\n" + cfMsg);
				}
				else
				{
					throw new Exception(cfMsg);
				}
			}
		}

		// Ref: https://api.cloudflare.com/#dns-records-for-a-zone-update-dns-record
		public async Task<DnsUpdateResponse> UpdateDnsAsync(IpSupport protocol, string zoneIdentifier, string dnsRecordIdentifier, string dnsRecordType, string dnsRecordName, string content, int ttl, bool proxied)
		{
			var dnsUpdateRequest = new DnsUpdateRequest() { type = dnsRecordType, name = dnsRecordName, content = content, ttl = ttl, proxied = proxied };

			HttpResponseMessage response;

			HttpRequestMessage req = await GetRequestMessageAsync(HttpMethod.Put, $"zones/{zoneIdentifier}/dns_records/{dnsRecordIdentifier}");
			req.Content = new StringContent(JsonSerializer.Serialize(dnsUpdateRequest), Encoding.UTF8, "application/json");

			var httpClient = _httpClientFactory.CreateClient(HttpClientName.Cloudflare.ToString());
			response = await httpClient.SendAsync(req);
			var result = await response.Content.ReadAsStringAsync();

			await ValidateCloudflareResultAsync(response, result, $"update {protocol} DNS");

			var ret = JsonSerializer.Deserialize<DnsUpdateResponse>(result);
			return ret;
		}

		public async Task<List<Result>> GetAllDnsRecordsByZoneAsync()
		{
			var settings = await _settingsService.GetAsync();
			var zoneIDs = settings.ZoneIDs?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			if (zoneIDs is null || !zoneIDs.Any())
				zoneIDs = await ListZoneIDsAsync();

			var allDnsEntries = new List<Result>();

			foreach (var zoneID in zoneIDs)
			{
				if (settings.ProtocolSupport != IpSupport.IPv6)
				{
					var aRecords = await GetRecordsByTypeAsync(zoneID, "A");
					allDnsEntries.AddRange(aRecords);
				}

				if (settings.ProtocolSupport != IpSupport.IPv4)
				{
					var aaaaRecords = await GetRecordsByTypeAsync(zoneID, "AAAA");
					allDnsEntries.AddRange(aaaaRecords);
				}
				
				var txtRecords = await GetRecordsByTypeAsync(zoneID, "TXT");
				allDnsEntries.AddRange(txtRecords);

				var spfRecords = await GetRecordsByTypeAsync(zoneID, "SPF");
				allDnsEntries.AddRange(spfRecords);
			}

			return allDnsEntries.Distinct().ToList();
		}

		private async Task<HttpRequestMessage> GetRequestMessageAsync(HttpMethod httpMethod, string requestUri)
		{
			var settings = await _settingsService.GetAsync();
			var req = new HttpRequestMessage(httpMethod, requestUri);

			if (settings.IsUsingToken)
			{
				req.Headers.Add("Authorization", " Bearer " + settings.ApiKeyOrToken);
			}
			else
			{
				req.Headers.Add("X-Auth-Key", settings.ApiKeyOrToken);
				req.Headers.Add("X-Auth-Email", settings.EmailAddress);
			}
			return req;
		}

		public async Task<bool> ValidateSelectedDomainsAsync()
		{
			var settings = await _settingsService.GetAsync();

			if (!settings.SelectedDomains.Any(entry => entry.Type != null))
			{
				_logger.LogWarning("No domains selected. Please select the entries that you would like to update at http://localhost:5666");
				await _logService.WriteAsync("No domains selected. Please select the entries that you would like to update.", LogLevel.Warning);

				// remove invalid entries
				settings.SelectedDomains.RemoveAll(entry => entry.Type == null);

				return false;
			}

			return true;
		}
	}

	public class CloudflareApiError
	{
		public bool success { get; set; }
		public Error[] errors { get; set; }
		public object[] messages { get; set; }
		public object result { get; set; }

		public class Error
		{
			public int code { get; set; }
			public string message { get; set; }
		}
	}
}
