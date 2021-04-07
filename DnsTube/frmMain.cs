using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DnsTube
{
	public partial class frmMain : Form
	{
		private HttpClient httpClient;
		private CloudflareAPI cfClient;
		private Settings settings;
		private string RELEASE_TAG = "v0.8.1";

		public frmMain()
		{
			InitializeComponent();

			settings = new Settings();
		}

		private void frmMain_Load(object sender, EventArgs e)
		{
			Init();

			DisplayVersion();

			PromptForSettings();

			cfClient = new CloudflareAPI(httpClient, settings);

			UpdateList();

			DisplayAndLogPublicIpAddress();

			if (validateSettings())
				ValidateSelectedDomains();

			ScheduleUpdates();
		}

		private void ValidateSelectedDomains()
		{
			// handle old settings where we did not save Type, or nothing selected at all
			if (!settings.SelectedDomains.Any(entry => entry.Type != null))
			{
				MessageBox.Show("Please select the entries that you would like to update");

				// remove invalid entries
				settings.SelectedDomains.RemoveAll(entry => entry.Type == null);
			}
		}

		private void DisplayAndLogPublicIpAddress()
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

		private void ScheduleUpdates()
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

		private void DoUpdate()
		{
			if (!PreflightSettingsCheck())
				return;

			var updatedAddress = false;
			// if IPv6-only support was not specified, do the IPv4 update
			if (settings.ProtocolSupport != IpSupport.IPv6)
				if (UpdateCloudflareDns(IpSupport.IPv4))
					updatedAddress = true;

			// if IPv4-only support was not specified, do the IPv6 update
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
				AppendStatusTextThreadSafe($"Error detecting public {protocol} address");
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
					AppendStatusTextThreadSafe($"Public {protocol} changed from {oldPublicIpAddress} to {publicIpAddress}");

				DisplayPublicIpAddressThreadSafe(protocol, publicIpAddress);

				var typesToUpdateForThisProtocol = new List<string> {
					"SPF",
					"TXT",
					protocol == IpSupport.IPv4 ? "A" : "AAAA"
				};

				// Get requested entries to update
				List<Dns.Result> potentialEntriesToUpdate = null;
				try
				{
					var allRecordsByZone = cfClient.GetAllDnsRecordsByZone();

					potentialEntriesToUpdate = allRecordsByZone.Where(d => settings.SelectedDomains.Any(s =>
						s.ZoneName == d.zone_name
						&& s.DnsName == d.name
						&& s.Type == d.type)
						&& typesToUpdateForThisProtocol.Contains(d.type)).ToList();
				}
				catch (Exception ex)
				{
					AppendStatusTextThreadSafe($"Error getting DNS records");
					AppendStatusTextThreadSafe(ex.Message);
				}

				// TODO:determine which ones need updating

				if (potentialEntriesToUpdate == null || !potentialEntriesToUpdate.Any())
					return false;

				foreach (var entry in potentialEntriesToUpdate)
				{
					string content;
					if (entry.type == "SPF" || entry.type == "TXT")
						content = UpdateContent(protocol, entry.content, publicIpAddress);
					else
						content = publicIpAddress;

					if (entry.content == content)
						continue;

					try
					{
						cfClient.UpdateDns(protocol, entry.zone_id, entry.id, entry.type, entry.name, content, entry.proxied);
						txtOutput.Invoke((MethodInvoker)delegate
						{
							AppendStatusTextThreadSafe($"Updated {entry.type} record [{entry.name}] in zone [{entry.zone_name}] to {content}");
						});
					}
					catch (Exception ex)
					{
						AppendStatusTextThreadSafe($"Error updating [{entry.type}] record [{entry.name}] in zone [{entry.zone_name}] to {content}");
						AppendStatusTextThreadSafe(ex.Message);
					}
				}
				return true;
			}

			return false;
		}

		private void PromptForSettings()
		{
			if (!validateSettings())
			{
				MessageBox.Show($"Please configure your settings.");
				DisplaySettingsForm();
			}
		}

		private bool PreflightSettingsCheck()
		{
			if (!validateSettings())
			{
				AppendStatusTextThreadSafe($"Settings not configured");
				return false;
			}
			return true;
		}

		private bool validateSettings()
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

		private string GetPublicIpAddress(IpSupport protocol)
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

		private void Init()
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

			var release = Utility.GetLatestRelease();
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

		private void btnUpdateList_Click(object sender, EventArgs e)
		{
			UpdateList();
		}

		private void UpdateList()
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

		private void UpdateListView(List<Dns.Result> allDnsRecords)
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
						row.SubItems.Add(dnsRecord.type);
						row.SubItems.Add(dnsRecord.name);
						row.SubItems.Add(dnsRecord.content);
						row.SubItems.Add(dnsRecord.proxied ? "Yes" : "No");
						row.Tag = zone;

						if (settings.SelectedDomains.Any(entry =>
								entry.ZoneName == dnsRecord.zone_name
								&& entry.DnsName == dnsRecord.name
								&& entry.Type == dnsRecord.type))
							row.Checked = true;

						listViewRecords.Items.Add(row);
					}
				}
			});
		}

		private void btnQuit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void listViewRecords_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			if (listViewRecords.FocusedItem == null)
				return;

			ListViewItem item = e.Item;
			string itemDnsEntryName = item.SubItems[colName.Index].Text;
			string itemZoneName = item.Tag.ToString();
			string itemType = item.SubItems[colType.Index].Text;

			if (!item.Checked)
			{
				// Make sure to clean up any old entries in the settings
				settings.SelectedDomains.RemoveAll(entry => entry.ZoneName == itemZoneName && entry.DnsName == itemDnsEntryName && entry.Type == itemType);
				settings.Save();
			}
			else
			{
				// Item has been selected by the user, store it for later
				if (settings.SelectedDomains.Any(entry => entry.ZoneName == itemZoneName && entry.DnsName == itemDnsEntryName && entry.Type == itemType))
				{
					// Item is already in the settings list, do nothing.
					return;
				}
				settings.SelectedDomains.Add(new SelectedDomain() { ZoneName = itemZoneName, DnsName = itemDnsEntryName, Type = itemType });
				settings.Save();
			}
		}

		private void btnSettings_Click(object sender, EventArgs e)
		{
			DisplaySettingsForm();
		}

		private void DisplaySettingsForm()
		{
			var frm = new frmSettings(settings);
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

		private void AppendStatusTextThreadSafe(string s)
		{
			txtOutput.Invoke((MethodInvoker)delegate
			{
				txtOutput.Text += $"{Utility.GetDateString()}: {s}\r\n"; // Running on the UI thread
			});
		}

		private void DisplayPublicIpAddressThreadSafe(IpSupport protocol, string s)
		{
			TextBox input = protocol == IpSupport.IPv4 ? txtPublicIpv4 : txtPublicIpv6;
			input.Invoke((MethodInvoker)delegate
			{
				input.Text = s; // Running on the UI thread
			});
		}

		private void SetNextUpdateTextThreadSafe(DateTime d)
		{
			txtNextUpdate.Invoke((MethodInvoker)delegate
			{
				txtNextUpdate.Text = d.ToString("h:mm:ss tt"); // Running on the UI thread
			});
		}

		private void DisplayVersion()
		{
			var execAssembly = System.Reflection.Assembly.GetExecutingAssembly();
			var version = execAssembly.GetName().Version.ToString();
			Text = $"DnsTube v{version}";
			AppendStatusTextThreadSafe(Text);
			if (File.Exists(settings.GetSettingsFilePath()))
				AppendStatusTextThreadSafe($"Settings path: {settings.GetSettingsFilePath()}");
		}

		private void btnUpdate_Click(object sender, EventArgs e)
		{
			AppendStatusTextThreadSafe($"Manually updating IP address");
			DoUpdate();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			TaskScheduler.StopAll();

			base.OnClosing(e);
		}

		private const string ipv4Regex = @"\b(?:(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\b";
		private const string ipv6Regex = @"\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%.+)?\s*";

		private string UpdateContent(IpSupport protocol, string content, string publicIpAddress)
		{
			// we need explicit protocol in this method
			if (protocol == IpSupport.IPv4AndIPv6)
				throw new ArgumentOutOfRangeException();

			var newContent = content;

			if (protocol == IpSupport.IPv4)
				newContent = Regex.Replace(newContent, ipv4Regex, publicIpAddress);
			else if (protocol == IpSupport.IPv6)
				newContent = Regex.Replace(newContent, ipv6Regex, publicIpAddress);

			return newContent;
		}
	}
}
