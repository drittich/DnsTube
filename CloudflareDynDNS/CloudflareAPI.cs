using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CloudflareDynDNS.Dns;
using Newtonsoft.Json;
using CloudflareDynDNS.Zone;
using System.Collections.Generic;

namespace CloudflareDynDNS
{
	public class CloudflareAPI
	{
		public string ApiKey { get; set; }
		public string Email { get; set; }

		public static string EndPoint = "https://api.cloudflare.com/client/v4/";
		public HttpClient Client { get; set; }

		public CloudflareAPI(HttpClient client)
		{
			Client = client;
			Client.BaseAddress = new Uri(EndPoint);
		}

		// Ref: https://api.cloudflare.com/#zone-list-zones
		public ListZonesResponse ListZones()
		{
			HttpRequestMessage req = GetRequestMessage(HttpMethod.Get, "zones?status=active&page=1&per_page=50&order=name&direction=asc&match=all");

			Client.DefaultRequestHeaders
				  .Accept
				  .Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var response = Client.SendAsync(req).Result;
			response.EnsureSuccessStatusCode(); // throws if response.IsSuccessStatusCode == false

			var result = response.Content.ReadAsStringAsync().Result;
			var ret = JsonConvert.DeserializeObject<Zone.ListZonesResponse>(result);
			return ret;
		}

		// Ref: https://api.cloudflare.com/#dns-records-for-a-zone-list-dns-records
		public DnsRecordsResponse ListDnsRecords(string zoneIdentifier)
		{
			Client.DefaultRequestHeaders
				.Accept
				.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var req = GetRequestMessage(HttpMethod.Get, $"zones/{zoneIdentifier}/dns_records?type=A&page=1&per_page=100&order=name&direction=asc&match=all");

			var response = Client.SendAsync(req).Result;
			response.EnsureSuccessStatusCode(); // throws if response.IsSuccessStatusCode == false

			var result = response.Content.ReadAsStringAsync().Result;
			var ret = JsonConvert.DeserializeObject<DnsRecordsResponse>(result);
			return ret;
		}

		// Ref: https://api.cloudflare.com/#dns-records-for-a-zone-update-dns-record
		public DnsUpdateResponse UpdateDns(string zoneIdentifier, string dnsRecordIdentifier, string dnsRecordName, string content)
		{
			var dnsUpdateRequest = new DnsUpdateRequest() { type = "A", name = dnsRecordName, content = content };

			HttpResponseMessage response = null;

			HttpRequestMessage req = GetRequestMessage(HttpMethod.Put, $"zones/{zoneIdentifier}/dns_records/{dnsRecordIdentifier}");
			req.Content = new StringContent(JsonConvert.SerializeObject(dnsUpdateRequest), Encoding.UTF8, "application/json");

			response = Client.SendAsync(req).Result;
			response.EnsureSuccessStatusCode(); // throws if response.IsSuccessStatusCode == false

			var result = response.Content.ReadAsStringAsync().Result;
			var ret = JsonConvert.DeserializeObject<DnsUpdateResponse>(result);
			return ret;
		}

		public List<Dns.Result> GetAllDnsRecordsByZone()
		{
			var allDnsEntries = new List<Dns.Result>();
			ListZonesResponse zones = ListZones();

			foreach (var zone in zones.result)
			{
				var dnsRecords = ListDnsRecords(zone.id);
				allDnsEntries.AddRange(dnsRecords.result);
			}

			return allDnsEntries;
		}

		HttpRequestMessage GetRequestMessage(HttpMethod httpMethod, string requestUri)
		{
			var req = new HttpRequestMessage(httpMethod, requestUri);
			req.Headers.Add("X-Auth-Email", Utility.GetSetting("Email"));
			req.Headers.Add("X-Auth-Key", Utility.GetSetting("ApiKey"));
			return req;
		}
	}
}
