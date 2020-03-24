using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace DnsTube
{
	public partial class frmMain : Form
	{

		HttpClient httpClient;
		CloudflareAPI cfClient;
		Settings settings;
		TelemetryClient tc = new TelemetryClient();
		string RELEASE_TAG = "v0.7.2";
		string AI_INSTRUMENTATION_KEY = "";

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

			cfClient = new CloudflareAPI(httpClient, settings);

			UpdateList();

			DisplayAndLogPublicIpAddress();

			ScheduleUpdates();
		}

		void DisplayAndLogPublicIpAddress()
		{
			if (settings.ProtocolSupport != IpSupport.IPv6)
			{
				var publicIpv4Address = GetPublicIpAddress(IpSupport.IPv4);
				if (publicIpv4Address == null)
					AppendStatusTextThreadSafe($"Error detecting public IPv4 address");
				else
					AppendStatusTextThreadSafe($"Detected public IPv4 {publicIpv4Address}");
			}
			if (settings.ProtocolSupport != IpSupport.IPv4)
			{
				var publicIpv6Address = GetPublicIpAddress(IpSupport.IPv6);
				if (publicIpv6Address == null)
					AppendStatusTextThreadSafe($"Error detecting public IPv6 address");
				else
					AppendStatusTextThreadSafe($"Detected public IPv6 {publicIpv6Address}");
			}
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

			var updatedAddress = false;
			if (settings.ProtocolSupport != IpSupport.IPv6)
				if (UpdateCloudflareDns(IpSupport.IPv4))
					updatedAddress = true;

			if (settings.ProtocolSupport != IpSupport.IPv4)
				if (UpdateCloudflareDns(IpSupport.IPv6))
					updatedAddress = true;

			// fetch and update listview with current status of records if necessary
			if (updatedAddress)
				UpdateList();
		}

		private bool UpdateCloudflareDns(IpSupport protocol)
		{
			var publicIpAddress = GetPublicIpAddress(protocol);
			if (publicIpAddress == null)
			{
				AppendStatusTextThreadSafe($"Error detecting public {protocol.ToString()} address");
				return false;
			}

			var oldPublicIpAddress = protocol == IpSupport.IPv4 ? settings.PublicIpv4Address : settings.PublicIpv6Address;
			if (publicIpAddress != oldPublicIpAddress)
			{
				if (protocol == IpSupport.IPv4)
					settings.PublicIpv4Address = publicIpAddress;
				else
					settings.PublicIpv6Address = publicIpAddress;
				settings.Save();

				if (oldPublicIpAddress != null)
					AppendStatusTextThreadSafe($"Public {protocol.ToString()} changed from {oldPublicIpAddress} to {publicIpAddress}");

				DisplayPublicIpAddressThreadSafe(protocol, publicIpAddress);

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
					tc.TrackException(ex);
				}

				if (entriesToUpdate == null)
					return false;

				foreach (var entry in entriesToUpdate)
				{
					try
					{
						cfClient.UpdateDns(protocol, entry.zone_id, entry.id, entry.name, publicIpAddress, entry.proxied);
						txtOutput.Invoke((MethodInvoker)delegate
						{
							AppendStatusTextThreadSafe($"Updated name [{entry.name}] in zone [{entry.zone_name}] to {publicIpAddress}");
						});
					}
					catch (Exception ex)
					{
						AppendStatusTextThreadSafe($"Error updating [{entry.name}] in zone [{entry.zone_name}] to {publicIpAddress}");
						AppendStatusTextThreadSafe(ex.Message);
						tc.TrackException(ex);
					}
				}
				return true;
			}

			return false;
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

		string GetPublicIpAddress(IpSupport protocol)
		{
			string errorMesssage;
			var publicIpAddress = Utility.GetPublicIpAddress(protocol, httpClient, out errorMesssage);

			// Abort if we get an error, keeping the current address in settings
			if (publicIpAddress == null)
			{
				AppendStatusTextThreadSafe($"Error getting public {protocol.ToString()}: {errorMesssage}");
				return null;
			}

			if ((protocol == IpSupport.IPv4 && txtPublicIpv4.Text != publicIpAddress) || (protocol == IpSupport.IPv6 && txtPublicIpv6.Text != publicIpAddress))
				DisplayPublicIpAddressThreadSafe(protocol, publicIpAddress);

			return publicIpAddress;
		}

		void Init()
		{
			if (settings.StartMinimized)
			{
				//Hide();
				WindowState = FormWindowState.Minimized;
			}

			httpClient = new HttpClient();

			// use TLS 1.2
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

			SetProtocolUiEnabled();
			TelemetryConfiguration.Active.InstrumentationKey = AI_INSTRUMENTATION_KEY;
			TelemetryConfiguration.Active.TelemetryInitializers.Add(new MyTelemetryInitializer());
			tc.Context.Session.Id = Guid.NewGuid().ToString();
			tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
			tc.TrackPageView("frmMain");

			var release = Utility.GetLatestRelease(tc);
			if (release != null && release.tag_name != RELEASE_TAG && !settings.SkipCheckForNewReleases)
			{
				if (MessageBox.Show($"A new version of DnsTube is available for download. \n\nClick Yes to view the latest release, or No to ignore.", "DnsTube Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
				{
					System.Diagnostics.Process.Start("https://github.com/drittich/DnsTube/releases/latest");
				}
			}
		}

		private void SetProtocolUiEnabled()
		{
			lblPublicIpv4Address.Enabled = settings.ProtocolSupport != IpSupport.IPv6;
			txtPublicIpv4.Enabled = settings.ProtocolSupport != IpSupport.IPv6;

			lblPublicIpv6Address.Enabled = settings.ProtocolSupport != IpSupport.IPv4;
			txtPublicIpv6.Enabled = settings.ProtocolSupport != IpSupport.IPv4;
		}

		void btnUpdateList_Click(object sender, EventArgs e)
		{
			UpdateList();
			tc.TrackEvent("btnUpdateList_Click");

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
				tc.TrackException(e);
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
			var frm = new frmSettings(settings, tc);
			frm.ShowDialog();
			frm.Close();
			// reload settings
			settings = new Settings();

			SetProtocolUiEnabled();

			// pick up new credentials if they were changed
			cfClient = new CloudflareAPI(httpClient, settings);
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

		void DisplayPublicIpAddressThreadSafe(IpSupport protocol, string s)
		{
			TextBox input = protocol == IpSupport.IPv4 ? txtPublicIpv4 : txtPublicIpv6;
			input.Invoke((MethodInvoker)delegate
			{
				input.Text = s; // Running on the UI thread
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
			tc.TrackEvent("btnUpdate_Click");
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			TaskScheduler.StopAll();
			
			if (tc != null)
			{
				tc.Flush();
				// Allow time for flushing:
				System.Threading.Thread.Sleep(1000);
			}
			base.OnClosing(e);
		}
	}
}
