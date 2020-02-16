using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;

namespace DnsTube
{
	public partial class frmMain : Form
	{

		HttpClient Client;
		CloudflareAPI cfClient;
		Settings settings;

		public frmMain()
		{
			InitializeComponent();

			settings = new Settings();
		}

		void frmMain_Load(object sender, EventArgs e)
		{
			Init();

			DisplayVersion();

			PromptForSettings();

			cfClient = new CloudflareAPI(Client, settings);

			UpdateList();

			DisplayAndLogPublicIpAddress();

			ScheduleUpdates();
		}

		void DisplayAndLogPublicIpAddress()
		{
			var publicIpAddress = GetPublicIpAddress();
			if (publicIpAddress == null)
				AppendStatusTextThreadSafe($"Error detecting public IP address");
			else
				AppendStatusTextThreadSafe($"Detected public IP {publicIpAddress}");
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
						DoUpdate();
					}
					finally
					{
						SetNextUpdateTextThreadSafe(DateTime.Now.Add(interval));
					}
				});
		}

		void DoUpdate()
		{
			if (!PreflightSettingsCheck())
				return;

			var publicIpAddress = GetPublicIpAddress();
			if (publicIpAddress == null)
			{
				AppendStatusTextThreadSafe($"Error detecting public IP address");
				return;
			}

			var oldPublicIpAddress = settings.PublicIpAddress;
			if (publicIpAddress != oldPublicIpAddress)
			{
				settings.PublicIpAddress = publicIpAddress;
				settings.Save();

				if (oldPublicIpAddress != null)
					AppendStatusTextThreadSafe($"Public IP changed from {oldPublicIpAddress} to {publicIpAddress}");

				DisplayPublicIpAddressThreadSafe(publicIpAddress);

				// loop through DNS entries and update the ones selected that have a different IP
				List<Dns.Result> entriesToUpdate = null;
				try
				{
					entriesToUpdate = cfClient.GetAllDnsRecordsByZone().Where(d => settings.SelectedDomains
						.Any(s => s.ZoneName == d.zone_name && s.DnsName == d.name && d.content != publicIpAddress)).ToList();
				}
				catch (Exception ex)
				{
					AppendStatusTextThreadSafe($"Error getting DNS records");
					AppendStatusTextThreadSafe(ex.Message);
				}

				if (entriesToUpdate == null)
					return;

				foreach (var entry in entriesToUpdate)
				{
					try
					{
						cfClient.UpdateDns(entry.zone_id, entry.id, entry.name, publicIpAddress, entry.proxied);
						txtOutput.Invoke((MethodInvoker)delegate
						{
							AppendStatusTextThreadSafe($"Updated name [{entry.name}] in zone [{entry.zone_name}] to {publicIpAddress}");
						});
					}
					catch (Exception ex)
					{
						AppendStatusTextThreadSafe($"Error updating [{entry.name}] in zone [{entry.zone_name}] to {publicIpAddress}");
						AppendStatusTextThreadSafe(ex.Message);
					}
				}

				// fetch and update listview with current status of records
				UpdateList();
			}
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
				AppendStatusTextThreadSafe($"Settings not configured");
				return false;
			}
			return true;
		}

		bool validateSettings()
		{
			// check if settings are populate
			if (string.IsNullOrWhiteSpace(settings.EmailAddress)
				|| !settings.EmailAddress.Contains("@")
				|| (!settings.IsUsingToken && string.IsNullOrWhiteSpace(settings.ApiKey))
				|| (settings.IsUsingToken && string.IsNullOrWhiteSpace(settings.ApiToken))
				|| settings.UpdateIntervalMinutes == 0
				)
				return false;
			
			return true;
		}

		string GetPublicIpAddress()
		{
			string errorMesssage;
			var publicIpAddress = Utility.GetPublicIpAddress(Client, out errorMesssage);

			// Bail if failed, keeping the current address in settings
			if (publicIpAddress == null)
			{
				AppendStatusTextThreadSafe($"Error getting public IP: {errorMesssage}");
				return null;
			}

			if (txtPublicIpAddress.Text != publicIpAddress)
				DisplayPublicIpAddressThreadSafe(publicIpAddress);

			return publicIpAddress;
		}

		void Init()
		{
			if (settings.StartMinimized)
			{
				//Hide();
				WindowState = FormWindowState.Minimized;
			}

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
				AppendStatusTextThreadSafe($"Error fetching list: {e.Message}");
				if (settings.IsUsingToken && e.Message.Contains("403 (Forbidden)"))
					AppendStatusTextThreadSafe($"Make sure your token has Zone:Read permissions. See https://dash.cloudflare.com/profile/api-tokens to configure.");
			}
		}

		void UpdateListView(List<Dns.Result> allDnsRecords)
		{
			listViewRecords.Invoke((MethodInvoker)delegate
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
						row.SubItems.Add(dnsRecord.proxied ? "Yes" : "No");
						row.Tag = zone;

						if (settings.SelectedDomains.Any(entry => entry.ZoneName == dnsRecord.zone_name && entry.DnsName == dnsRecord.name))
							row.Checked = true;

						listViewRecords.Items.Add(row);
					}
				}
			});
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
			cfClient = new CloudflareAPI(Client, settings);
			// pick up new interval if it was changed
			ScheduleUpdates();
		}

		void AppendStatusTextThreadSafe(string s)
		{
			txtOutput.Invoke((MethodInvoker)delegate
			{
				txtOutput.Text += $"{Utility.GetDateString()}: {s}\r\n"; // Running on the UI thread		
			});
		}

		void DisplayPublicIpAddressThreadSafe(string s)
		{
			txtPublicIpAddress.Invoke((MethodInvoker)delegate
			{
				txtPublicIpAddress.Text = s; // Running on the UI thread
			});
		}

		void SetNextUpdateTextThreadSafe(DateTime d)
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
			Text = $"DnsTube v{version}";
			var compileDate = execAssembly.GetLinkerTime().ToString("yyyy-MM-dd");
			AppendStatusTextThreadSafe($"DnsTube v{version} ({compileDate})");
			if (File.Exists(settings.GetSettingsFilePath()))
				AppendStatusTextThreadSafe($"Settings path: {settings.GetSettingsFilePath()}");
		}

		void frmMain_Resize(object sender, EventArgs e)
		{
			notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;

			if (FormWindowState.Minimized == this.WindowState)
			{
				notifyIcon1.Visible = true;
				notifyIcon1.ShowBalloonTip(500);
				this.Hide();
				this.ShowInTaskbar = false;
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
			this.ShowInTaskbar = true;
			this.WindowState = FormWindowState.Normal;
		}

		void btnUpdate_Click(object sender, EventArgs e)
		{
			AppendStatusTextThreadSafe($"Manually updating IP address");
			DoUpdate();
		}
	}
}
