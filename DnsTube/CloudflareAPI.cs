using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using DnsTube.Dns;

using Newtonsoft.Json;

namespace DnsTube
{
	public class CloudflareAPI
	{
		private Settings settings;
		public static string EndPoint = "https://api.cloudflare.com/client/v4/";
		public HttpClient Client { get; set; }

		public CloudflareAPI(HttpClient client, Settings settings)
		{
			Client = client;
			if (Client.BaseAddress == null)
				Client.BaseAddress = new Uri(EndPoint);
			this.settings = settings;
		}

		/// <summary>
		/// Returns a list of zone IDs
		/// Ref: https://api.cloudflare.com/#zone-list-zones
		/// </summary>
		/// <returns></returns>
		public List<string> ListZoneIDs()
		{
			List<string> ret = new List<string>();
			int pageSize = 50;
			int pageNumber = 1;
			int totalPages;

			do
			{
				HttpRequestMessage req = GetRequestMessage(HttpMethod.Get, $"zones?status=active&page={pageNumber}&per_page={pageSize}&order=name&direction=asc&match=all");

				Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = Client.SendAsync(req).Result;
				var result = response.Content.ReadAsStringAsync().Result;

				ValidateCloudflareResult(response, result, "list zones");

				var zoneListResponse = JsonConvert.DeserializeObject<Zone.ListZonesResponse>(result);

				int totalRecords = zoneListResponse.result_info.total_count;
				totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

				ret.AddRange(zoneListResponse.result.Select(z => z.id));

				pageNumber++;
			} while (pageNumber <= totalPages);

			return ret;
		}

		// Ref: https://api.cloudflare.com/#dns-records-for-a-zone-list-dns-records
		private List<Result> GetRecordsByType(string zoneIdentifier, string recordType)
		{
			int pageSize = 100;
			int pageNumber = 1;
			int totalPages;

			var ret = new List<Result>();

			do
			{
				Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var req = GetRequestMessage(HttpMethod.Get, $"zones/{zoneIdentifier}/dns_records?type={recordType}&page={pageNumber}&per_page={pageSize}&order=name&direction=asc&match=all");

				var response = Client.SendAsync(req).Result;
				var result = response.Content.ReadAsStringAsync().Result;

				ValidateCloudflareResult(response, result, "list DNS records");

				var dnsRecordsResponse = JsonConvert.DeserializeObject<DnsRecordsResponse>(result);

				int totalRecords = dnsRecordsResponse.result_info.total_count;
				totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

				ret.AddRange(dnsRecordsResponse.result);

				pageNumber++;
			} while (pageNumber <= totalPages);

			return ret;
		}

		private void ValidateCloudflareResult(HttpResponseMessage response, string result, string action)
		{
			if (!response.IsSuccessStatusCode)
			{
				if (settings.IsUsingToken)
				{
					throw new Exception($"Unable to {action}. If you are updating all zones, token permissions should be similar to [All zones - Zone:Read, DNS:Edit]. If your token only has permissions for specific zones, click Settings and configure the Zone IDs with a comma-separated list.");
				}
				else
				{
					var cfError = JsonConvert.DeserializeObject<CloudflareApiError>(result);
					throw new Exception(cfError.errors?.FirstOrDefault().message);
				}
			}
		}

		// Ref: https://api.cloudflare.com/#dns-records-for-a-zone-update-dns-record
		public DnsUpdateResponse UpdateDns(IpSupport protocol, string zoneIdentifier, string dnsRecordIdentifier, string dnsRecordName, string content, bool proxied)
		{
			var recordType = protocol == IpSupport.IPv4 ? "A" : "AAAA";
			var dnsUpdateRequest = new DnsUpdateRequest() { type = recordType, name = dnsRecordName, content = content, proxied = proxied };

			HttpResponseMessage response;

			HttpRequestMessage req = GetRequestMessage(HttpMethod.Put, $"zones/{zoneIdentifier}/dns_records/{dnsRecordIdentifier}");
			req.Content = new StringContent(JsonConvert.SerializeObject(dnsUpdateRequest), Encoding.UTF8, "application/json");

			response = Client.SendAsync(req).Result;
			var result = response.Content.ReadAsStringAsync().Result;

			ValidateCloudflareResult(response, result, $"update {protocol.ToString()} DNS");

			var ret = JsonConvert.DeserializeObject<DnsUpdateResponse>(result);
			return ret;
		}

		public List<Dns.Result> GetAllDnsRecordsByZone()
		{
			var zoneIDs = settings.ZoneIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			if (!zoneIDs.Any())
				zoneIDs = ListZoneIDs();

			var allDnsEntries = new List<Dns.Result>();

			foreach (var zoneID in zoneIDs)
			{
				if (settings.ProtocolSupport != IpSupport.IPv6)
				{
					var aRecords = GetRecordsByType(zoneID, "A");
					allDnsEntries.AddRange(aRecords);
				}

				if (settings.ProtocolSupport != IpSupport.IPv4)
				{
					var aaaaRecords = GetRecordsByType(zoneID, "AAAA");
					allDnsEntries.AddRange(aaaaRecords);
				}

				var txtRecords = GetRecordsByType(zoneID, "TXT");
				allDnsEntries.AddRange(txtRecords);
			}

			return allDnsEntries.Distinct().ToList();
		}

		HttpRequestMessage GetRequestMessage(HttpMethod httpMethod, string requestUri)
		{
			var req = new HttpRequestMessage(httpMethod, requestUri);

			if (settings.IsUsingToken)
			{
				req.Headers.Add("Authorization", " Bearer " + settings.ApiToken);
			}
			else
			{
				req.Headers.Add("X-Auth-Key", settings.ApiKey);
				req.Headers.Add("X-Auth-Email", settings.EmailAddress);
			}
			return req;
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
