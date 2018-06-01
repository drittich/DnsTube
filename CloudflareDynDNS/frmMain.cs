using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Windows.Forms;
using CloudflareDynDNS.Dns;
using CloudflareDynDNS.Zone;
using Newtonsoft.Json;

namespace CloudflareDynDNS
{
	public partial class frmMain : frmBaseForm
	{
		string EndPoint = "https://api.cloudflare.com/client/v4/";

		HttpClient Client;

		public frmMain()
		{
			InitializeComponent();
			Init();
		}

		void Init()
		{
			Client = new HttpClient();
			Client.BaseAddress = new Uri(EndPoint);
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
		}

		void btnGo_Click(object sender, EventArgs e)
		{
			var externalAddress = GetExternalAddress();
			txtExternalAddress.Text = externalAddress;
			var zones = ListZones();

			var allDnsEntriesByZone = new Dictionary<string, DnsRecordsResponse>();
			foreach (var zone in zones.result)
				allDnsEntriesByZone[zone.name] = ListDnsRecords(zone.id);

			UpdateListView(allDnsEntriesByZone);
		}

		void UpdateListView(Dictionary<string, DnsRecordsResponse> allDnsEntries)
		{
			listViewRecords.Items.Clear();
			List<SelectedDnsEntry> selectedDnsEntries = GetSelectedEntries();

			foreach (var key in allDnsEntries.Keys)
			{
				var group = new ListViewGroup(key);
				listViewRecords.Groups.Add(group);
				var zoneDnsRecords = allDnsEntries[key].result;
				foreach (var dnsRecord in zoneDnsRecords)
				{
					var row = new ListViewItem(group);
					row.SubItems.Add(dnsRecord.name);
					row.SubItems.Add(dnsRecord.content);

					if (selectedDnsEntries.Any(entry => entry.ZoneName == dnsRecord.zone_name && entry.DnsEntryName == dnsRecord.name))
						row.Checked = true;

					listViewRecords.Items.Add(row);
				}
			}
		}

		static List<SelectedDnsEntry> GetSelectedEntries()
		{
			var ret = JsonConvert.DeserializeObject<List<SelectedDnsEntry>>(Properties.Settings.Default["SelectedDnsEntries"].ToString());
			return ret ?? new List<SelectedDnsEntry>();
		}

		// https://api.cloudflare.com/#zone-list-zones
		ListZonesResponse ListZones()
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

		// https://api.cloudflare.com/#dns-records-for-a-zone-list-dns-records
		DnsRecordsResponse ListDnsRecords(string zoneIdentifier)
		{
			Client.DefaultRequestHeaders
				.Accept
				.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			HttpRequestMessage req = GetRequestMessage(HttpMethod.Get, $"zones/{zoneIdentifier}/dns_records?type=A&page=1&per_page=100&order=name&direction=asc&match=all");

			var response = Client.SendAsync(req).Result;
			response.EnsureSuccessStatusCode(); // throws if response.IsSuccessStatusCode == false

			var result = response.Content.ReadAsStringAsync().Result;
			var ret = JsonConvert.DeserializeObject<Dns.DnsRecordsResponse>(result);
			return ret;
		}

		HttpRequestMessage GetRequestMessage(HttpMethod httpMethod, string requestUri)
		{
			var req = new HttpRequestMessage(httpMethod, requestUri);
			req.Headers.Add("X-Auth-Email", Utility.GetSetting("Email"));
			req.Headers.Add("X-Auth-Key", Utility.GetSetting("ApiKey"));
			return req;
		}

		void btnQuit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		//void UpdateDns()
		//{
		//	// https://api.cloudflare.com/#dns-records-for-a-zone-update-dns-record
		//	// PUT zones/:zone_identifier/dns_records/:identifier

		//	Dictionary<string, string> data = null;

		//	try
		//	{
		//		HttpResponseMessage response = null;

		//		var req = new HttpRequestMessage(HttpMethod.Put, EndPoint + "zones/023e105f4ecef8ad9ca31a8372d0c353/dns_records/372e67954025e0ba6aaa6d586b9e0b59") { Content = new FormUrlEncodedContent(data) };
		//		response = Client.SendAsync(req).Result;
		//		response.EnsureSuccessStatusCode(); // throws if response.IsSuccessStatusCode == false

		//		var result = response.Content.ReadAsStringAsync().Result;
		//		//if (LogResponse)
		//		//Console.WriteLine(result);

		//		//Console.WriteLine($"{RunID}: {api.Method.ToString()} {url + "?" + Utility.GetUrlEncodedParamString(data)} finished");
		//	}
		//	catch
		//	{
		//		//Console.Error.WriteLine($"{RunID}: {api.Method.ToString()} {url + "?" + Utility.GetUrlEncodedParamString(data)} error");
		//	}
		//}

		public string GetExternalAddress()
		{
			var req = GetRequestMessage(HttpMethod.Get, "http://checkip.dyndns.org");

			Client.DefaultRequestHeaders
				  .Accept
				  .Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var response = Client.SendAsync(req).Result;
			if (!response.IsSuccessStatusCode)
				return null;//Bail if failed, keeping the current address in settings
			if (response == null)
				return null; //Bail if failed, keeping the current address in settings

			var strResponse = response.Content.ReadAsStringAsync().Result;
			string[] strResponse2 = strResponse.Split(':');
			string strResponse3 = strResponse2[1].Substring(1);
			string newExternalAddress = strResponse3.Split('<')[0];

			if (newExternalAddress == null)
				return null; //Bail if failed, keeping the current address in settings

			if (newExternalAddress != Properties.Settings.Default["ExternalAddress"].ToString())
				Utility.SaveSetting("ExternalAddress", newExternalAddress);

			return newExternalAddress;
		}



		void listViewRecords_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (listViewRecords.FocusedItem == null)
				return;

			var selectedDnsEntries = GetSelectedEntries();

			ListViewItem listItem = e.Item;
			string listItemDnsEntryName = listItem.SubItems[1].Text;
			if (!listItem.Checked)
			{
				// Make sure to clean up any old entries in the settings
				selectedDnsEntries.RemoveAll(entry => entry.ZoneName == listItem.Group.Header && entry.DnsEntryName == listItemDnsEntryName);
			}
			else
			{
				// Item has been selected by the user, store it for later
				if (selectedDnsEntries.Any(entry => entry.ZoneName == listItem.Group.Header && entry.DnsEntryName == listItemDnsEntryName))
				{
					// Item is already in the settings list, do nothing.
					return;
				}
				selectedDnsEntries.Add(new SelectedDnsEntry() { ZoneName = listItem.Group.Header, DnsEntryName = listItemDnsEntryName });
			}

			Utility.SaveSetting("SelectedDnsEntries", JsonConvert.SerializeObject(selectedDnsEntries));
		}

		void btnSettings_Click(object sender, EventArgs e)
		{
			var frm = new frmSettings();
			frm.ShowDialog();
			frm.Close();
		}
	}
}
