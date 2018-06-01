using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows.Forms;
using CloudflareDynDNS.Dns;
using Newtonsoft.Json;

namespace CloudflareDynDNS
{
	public partial class frmMain : Form
	{

		HttpClient Client;
		CloudflareAPI cfClient;

		public frmMain()
		{
			InitializeComponent();
			foreach (Control c in Controls)
				c.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f, FontStyle.Regular, GraphicsUnit.Point);
		}

		void Init()
		{
			Client = new HttpClient();
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
			cfClient = new CloudflareAPI(Client);

			// move this to Loop code
			var externalAddress = Utility.GetExternalAddress(Client);
			// Bail if failed, keeping the current address in settings
			if (externalAddress != null)
			{
				if (externalAddress != Properties.Settings.Default["ExternalAddress"].ToString())
					Utility.SaveSetting("ExternalAddress", externalAddress);
			}

			txtExternalAddress.Text = externalAddress;
		}

		void btnGo_Click(object sender, EventArgs e)
		{
			var zones = cfClient.ListZones();

			var allDnsEntriesByZone = new Dictionary<string, DnsRecordsResponse>();
			foreach (var zone in zones.result)
				allDnsEntriesByZone[zone.name] = cfClient.ListDnsRecords(zone.id);

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







		void btnQuit_Click(object sender, EventArgs e)
		{
			Application.Exit();
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

		void frmMain_Load(object sender, EventArgs e)
		{
			Init();
		}

		void btnUpdateDNS_Click(object sender, EventArgs e)
		{
			cfClient.UpdateDns(null, null, null, null);
		}
	}
}
