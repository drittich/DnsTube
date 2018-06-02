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
		}

		void frmMain_Load(object sender, EventArgs e)
		{
			Init();

			PromptForSettings();

			DisplayVersion();

			UpdateList();

			AppendStatusText($"Detected public IP {GetExternalAddress()}");

			var interval = TimeSpan.FromMinutes(1);
			TaskScheduler.Instance.ScheduleTask(interval,
				() =>
				{
					try
					{
						if (!PreflightSettingsCheck())
							return;

						var externalAddress = GetExternalAddress();
						if (externalAddress == null)
							return;

						var oldExternalAddress = Properties.Settings.Default["ExternalAddress"].ToString();
						if (externalAddress != oldExternalAddress)
						{
							Utility.SaveSetting("ExternalAddress", externalAddress);

							this.txtOutput.Invoke((MethodInvoker)delegate
							{
								AppendStatusText($"Public IP changed from {oldExternalAddress} to {externalAddress}");
							});

							this.txtExternalAddress.Invoke((MethodInvoker)delegate
							{
								this.txtExternalAddress.Text = externalAddress; // Running on the UI thread
							});

							// loop through DNS entries and update the ones selected that have a different IP
							var savedSelectedEntries = GetSavedSelectedEntries();
							var entriesToUpdate = cfClient.GetAllDnsRecordsByZone().Where(d => savedSelectedEntries.Any(s => s.ZoneName == d.zone_name && s.DnsEntryName == d.name && d.content != externalAddress));
							foreach (var entry in entriesToUpdate)
							{
								cfClient.UpdateDns(entry.zone_id, entry.id, entry.name, externalAddress);
								this.txtOutput.Invoke((MethodInvoker)delegate
								{
									AppendStatusText($"Updated name [{entry.name}] in zone [{entry.zone_name}] to {externalAddress}");
								});
							}
						}
					}
					catch (Exception ex)
					{
						AppendStatusText($"Error detecting or updating IP address: {ex.Message}");
					}
				});
		}

		void PromptForSettings()
		{
			if (!validateSettings())
			{
				MessageBox.Show($"Please configure your settings.");
				var frm = new frmSettings();
				frm.ShowDialog();
				frm.Close();
			}
		}

		bool PreflightSettingsCheck()
		{
			if (!validateSettings())
			{
				AppendStatusText($"Settings not configured");
				return false;
			}
			return true;
		}

		bool validateSettings()
		{
			// check if settings are populate
			int updateInterval;
			if (string.IsNullOrWhiteSpace(Utility.GetSetting("Email")) || !Utility.GetSetting("Email").Contains("@")
				|| string.IsNullOrWhiteSpace(Utility.GetSetting("ApiKey"))
				|| !int.TryParse(Utility.GetSetting("UpdateInterval"), out updateInterval))
				return false;

			return true;

		}

		string GetExternalAddress()
		{
			var externalAddress = Utility.GetExternalAddress(Client);

			// Bail if failed, keeping the current address in settings
			if (externalAddress == null)
				return null;

			this.txtExternalAddress.Invoke((MethodInvoker)delegate
			{
				if (this.txtExternalAddress.Text != externalAddress)
					this.txtExternalAddress.Text = externalAddress; // Running on the UI thread
			});

			return externalAddress;
		}

		void Init()
		{
			Client = new HttpClient();

			// use TLS 1.2
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

			cfClient = new CloudflareAPI(Client);
		}

		void btnUpdateList_Click(object sender, EventArgs e)
		{
			UpdateList();
		}

		void UpdateList()
		{
			if (!PreflightSettingsCheck())
				return;

			try
			{
				var allDnsRecordsByZone = cfClient.GetAllDnsRecordsByZone();
				UpdateListView(allDnsRecordsByZone);
			}
			catch (Exception e)
			{
				AppendStatusText($"Error fetching list: {e.Message}");
			}
		}

		void UpdateListView(List<Dns.Result> allDnsRecords)
		{
			listViewRecords.Items.Clear();
			var savedSelectedDnsEntries = GetSavedSelectedEntries();

			// group each zones records under a separate header
			foreach (var zone in allDnsRecords.Select(d => d.zone_name).Distinct().OrderBy(z => z))
			{
				var group = new ListViewGroup("Zone: " + zone);
				listViewRecords.Groups.Add(group);
				var zoneDnsRecords = allDnsRecords.Where(d => d.zone_name == zone);
				foreach (var dnsRecord in zoneDnsRecords)
				{
					var row = new ListViewItem(group);
					row.SubItems.Add(dnsRecord.name);
					row.SubItems.Add(dnsRecord.content);

					if (savedSelectedDnsEntries.Any(entry => entry.ZoneName == dnsRecord.zone_name && entry.DnsEntryName == dnsRecord.name))
						row.Checked = true;

					listViewRecords.Items.Add(row);
				}
			}
		}

		static List<SelectedDnsEntry> GetSavedSelectedEntries()
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

			var savedSelectedDnsEntries = GetSavedSelectedEntries();

			ListViewItem listItem = e.Item;
			string listItemDnsEntryName = listItem.SubItems[1].Text;
			if (!listItem.Checked)
			{
				// Make sure to clean up any old entries in the settings
				savedSelectedDnsEntries.RemoveAll(entry => entry.ZoneName == listItem.Group.Header && entry.DnsEntryName == listItemDnsEntryName);
			}
			else
			{
				// Item has been selected by the user, store it for later
				if (savedSelectedDnsEntries.Any(entry => entry.ZoneName == listItem.Group.Header && entry.DnsEntryName == listItemDnsEntryName))
				{
					// Item is already in the settings list, do nothing.
					return;
				}
				savedSelectedDnsEntries.Add(new SelectedDnsEntry() { ZoneName = listItem.Group.Header, DnsEntryName = listItemDnsEntryName });
			}

			Utility.SaveSetting("SelectedDnsEntries", JsonConvert.SerializeObject(savedSelectedDnsEntries));
		}

		void btnSettings_Click(object sender, EventArgs e)
		{
			var frm = new frmSettings();
			frm.ShowDialog();
			frm.Close();
		}

		void btnUpdateDNS_Click(object sender, EventArgs e)
		{
			cfClient.UpdateDns(null, null, null, null);
		}

		void AppendStatusText(string s)
		{
			this.txtOutput.Invoke((MethodInvoker)delegate
			{
				this.txtOutput.Text += $"{Utility.GetDateString()}: {s}\r\n"; // Running on the UI thread		
			});
		}

		void DisplayVersion()
		{
			var execAssembly = System.Reflection.Assembly.GetExecutingAssembly();
			var version = execAssembly.GetName().Version.ToString();
			var compileDate = execAssembly.GetLinkerTime().ToString("yyyy-MM-dd");
			AppendStatusText($"Cloudflare DynDNS v{version} ({compileDate})");
		}
	}
}
