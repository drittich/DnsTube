using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows.Forms;
using CloudflareDynDNS.Dns;
using CloudflareDynDNS.Properties;
using Newtonsoft.Json;

namespace CloudflareDynDNS
{
	public partial class frmMain : Form
	{

		HttpClient Client;
		CloudflareAPI cfClient;
		Settings settings;

		public frmMain()
		{
			InitializeComponent();
		}

		void frmMain_Load(object sender, EventArgs e)
		{
			Init();

			DisplayVersion();

			PromptForSettings();

			cfClient = new CloudflareAPI(Client, settings.EmailAddress, settings.ApiKey);

			UpdateList();

			DisplayPublicIpAddress();

			ScheduleUpdates();
		}

		void DisplayPublicIpAddress()
		{
			var externalAddress = GetExternalAddress();
			if (externalAddress == null)
				AppendStatusText($"Error detecting public IP address");
			else
				AppendStatusText($"Detected public IP {externalAddress}");
		}

		void ScheduleUpdates()
		{
			var interval = TimeSpan.FromMinutes(settings.UpdateIntervalMinutes);
			txtNextUpdate.Text = DateTime.Now.Add(interval).ToString("h:mm:ss tt");

			TaskScheduler.StopAll();
			TaskScheduler.Instance.ScheduleTask(interval,
				() =>
				{
					try
					{
						if (!PreflightSettingsCheck())
							return;

						var externalAddress = GetExternalAddress();
						if (externalAddress == null)
						{
							AppendStatusText($"Error detecting public IP address");
							return; 
						}

						var oldExternalAddress = settings.PublicIpAddress;
						if (externalAddress != oldExternalAddress)
						{
							settings.PublicIpAddress = externalAddress;
							settings.Save();

							txtOutput.Invoke((MethodInvoker)delegate
							{
								if (oldExternalAddress != null)
									AppendStatusText($"Public IP changed from {oldExternalAddress} to {externalAddress}");
							});

							txtExternalAddress.Invoke((MethodInvoker)delegate
							{
								txtExternalAddress.Text = externalAddress; // Running on the UI thread
							});

							// loop through DNS entries and update the ones selected that have a different IP
							var entriesToUpdate = cfClient.GetAllDnsRecordsByZone().Where(d => settings.SelectedDomains
								.Any(s => s.ZoneName == d.zone_name && s.DnsName == d.name && d.content != externalAddress));
							foreach (var entry in entriesToUpdate)
							{
								cfClient.UpdateDns(entry.zone_id, entry.id, entry.name, externalAddress);
								txtOutput.Invoke((MethodInvoker)delegate
								{
									AppendStatusText($"Updated name [{entry.name}] in zone [{entry.zone_name}] to {externalAddress}");
								});
							}
						}
					}
					catch (Exception ex)
					{
						AppendStatusText($"Error detecting/updating IP address: {ex.Message}");
					}
					finally
					{
						SetNextUpdateText(DateTime.Now.Add(interval));
					}
				});
		}

		void PromptForSettings()
		{
			if (!validateSettings())
			{
				MessageBox.Show($"Please configure your settings.");
				DisplaySettingsForm();
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
			if (string.IsNullOrWhiteSpace(settings.EmailAddress)
				|| !settings.EmailAddress.Contains("@")
				|| string.IsNullOrWhiteSpace(settings.ApiKey)
				|| settings.UpdateIntervalMinutes == 0
				)
				return false;

			return true;

		}

		string GetExternalAddress()
		{
			var externalAddress = Utility.GetExternalAddress(Client);

			// Bail if failed, keeping the current address in settings
			if (externalAddress == null)
				return null;

			txtExternalAddress.Invoke((MethodInvoker)delegate
			{
				if (txtExternalAddress.Text != externalAddress)
					txtExternalAddress.Text = externalAddress; // Running on the UI thread
			});

			return externalAddress;
		}

		void Init()
		{
			settings = new Settings();
			Client = new HttpClient();

			// use TLS 1.2
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
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
					row.Tag = zone;

					if (settings.SelectedDomains.Any(entry => entry.ZoneName == dnsRecord.zone_name && entry.DnsName == dnsRecord.name))
						row.Checked = true;

					listViewRecords.Items.Add(row);
				}
			}
		}

		void btnQuit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		void listViewRecords_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (listViewRecords.FocusedItem == null)
				return;

			ListViewItem item = e.Item;
			string itemDnsEntryName = item.SubItems[1].Text;
			string itemZoneName = item.Tag.ToString();

			if (!item.Checked)
			{
				// Make sure to clean up any old entries in the settings
				settings.SelectedDomains.RemoveAll(entry => entry.ZoneName == itemZoneName && entry.DnsName == itemDnsEntryName);
				settings.Save();
			}
			else
			{
				// Item has been selected by the user, store it for later
				if (settings.SelectedDomains.Any(entry => entry.ZoneName == itemZoneName && entry.DnsName == itemDnsEntryName))
				{
					// Item is already in the settings list, do nothing.
					return;
				}
				settings.SelectedDomains.Add(new SelectedDomain() { ZoneName = itemZoneName, DnsName = itemDnsEntryName });
				settings.Save();
			}
		}

		void btnSettings_Click(object sender, EventArgs e)
		{
			DisplaySettingsForm();
		}

		void DisplaySettingsForm()
		{
			var frm = new frmSettings(settings);
			frm.ShowDialog();
			frm.Close();
			// reload settings
			settings = new Settings();
			// pick up new credentials if they were changed
			cfClient = new CloudflareAPI(Client, settings.EmailAddress, settings.ApiKey);
			// pick up new interval if it was changed
			ScheduleUpdates(); 
		}

		void btnUpdateDNS_Click(object sender, EventArgs e)
		{
			cfClient.UpdateDns(null, null, null, null);
		}

		void AppendStatusText(string s)
		{
			txtOutput.Invoke((MethodInvoker)delegate
			{
				txtOutput.Text += $"{Utility.GetDateString()}: {s}\r\n"; // Running on the UI thread		
			});
		}

		void SetNextUpdateText(DateTime d)
		{
			txtNextUpdate.Invoke((MethodInvoker)delegate
			{
				txtNextUpdate.Text = d.ToString("h:mm:ss tt"); // Running on the UI thread		
			});
		}

		void DisplayVersion()
		{
			var execAssembly = System.Reflection.Assembly.GetExecutingAssembly();
			var version = execAssembly.GetName().Version.ToString();
			var compileDate = execAssembly.GetLinkerTime().ToString("yyyy-MM-dd");
			AppendStatusText($"Cloudflare DynDNS v{version} ({compileDate})");
			if (File.Exists(settings.GetSettingsFilePath()))
				AppendStatusText($"Settings path: {settings.GetSettingsFilePath()}");
		}

		void frmMain_Resize(object sender, EventArgs e)
		{
			notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;

			if (FormWindowState.Minimized == this.WindowState)
			{
				notifyIcon1.Visible = true;
				notifyIcon1.ShowBalloonTip(500);
				this.Hide();
			}
			else if (FormWindowState.Normal == this.WindowState)
			{
				notifyIcon1.Visible = false;
			}
		}

		void notifyIcon1_Click(object sender, EventArgs e)
		{
			notifyIcon1.Visible = false;
			this.Show();
			this.WindowState = FormWindowState.Normal;
		}
	}
}
