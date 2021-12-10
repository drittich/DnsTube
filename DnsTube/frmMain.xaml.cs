using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using DnsTube.Core;

using Hardcodet.Wpf.TaskbarNotification;

namespace DnsTube
{
	/// <summary>
	/// Interaction logic for frmMain.xaml
	/// </summary>
	public partial class frmMain : Window
	{
		private static Mutex _mutex = null;
		private Engine engine;
		private Settings settings;
		ObservableCollection<DnsEntryViewItem> observableDnsEntryCollection;
		TaskbarIcon notifyIcon1;
		private bool isInitialMinimize = false;
		private CancellationTokenSource cancellationTokenSource;

		public frmMain()
		{
			const string appName = "DnsTube";
			Icon = BitmapFrame.Create(new Uri("pack://application:,,,/DnsTube.Gui;component/icon-48.ico"));
			bool isFirstInstance;
			_mutex = new Mutex(true, appName, out isFirstInstance);
			if (!isFirstInstance)
			{
				MessageBox.Show("DnsTube is already running", "DnsTube", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}

		private void InitNotifyIcon()
		{
			notifyIcon1 = new TaskbarIcon();
			notifyIcon1.Icon = new System.Drawing.Icon("icon-48.ico");
			notifyIcon1.ToolTipText = "DnsTube";
			notifyIcon1.Visibility = Visibility.Collapsed;
			notifyIcon1.TrayLeftMouseDown += NotifyIcon1_TrayMouseDown;
			notifyIcon1.TrayRightMouseDown += NotifyIcon1_TrayMouseDown;
		}

		private void NotifyIcon1_TrayMouseDown(object sender, RoutedEventArgs e)
		{
			ShowInTaskbar = true;
			WindowState = WindowState.Normal;
			Activate();
		}

		private void frmMain_StateChanged(object sender, EventArgs e)
		{
			if (WindowState == WindowState.Minimized && settings.MinimizeToTray)
			{
				notifyIcon1.Visibility = Visibility.Visible;
				WindowStyle = WindowStyle.ToolWindow;
				ShowInTaskbar = false;
				if (isInitialMinimize)
					isInitialMinimize = false;
				else
					notifyIcon1.ShowBalloonTip("DnsTube", "Application will continue to work in the background", BalloonIcon.Info);
			}
			else if (WindowState == WindowState.Normal)
			{
				notifyIcon1.Visibility = Visibility.Collapsed;
				WindowStyle = WindowStyle.SingleBorderWindow;
			}
		}

		private void frmMain_Loaded(object sender, RoutedEventArgs e)
		{
			Init();

			Title = $"DnsTube {engine.RELEASE_TAG}";
			AppendStatusText(Title);
			engine.DisplayVersionAndSettingsPath(AppendStatusText);

			PromptForSettings();

			FetchDsnEntries();

			engine.DisplayAndLogPublicIpAddress(settings, txtPublicIpv4.Text, txtPublicIpv6.Text, AppendStatusText, DisplayPublicIpAddressThreadSafe);

			if (settings.Validate())
				ValidateSelectedDomains();

			cancellationTokenSource = new CancellationTokenSource();
			ScheduleUpdates(cancellationTokenSource.Token);
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



		private async Task ScheduleUpdates(CancellationToken stoppingToken)
		{
			var interval = TimeSpan.FromMinutes(settings.UpdateIntervalMinutes);
			txtNextUpdate.Text = DateTime.Now.Add(interval).ToString("h:mm:ss tt");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					DoUpdate();
				}
				finally
				{
					SetNextUpdateTextThreadSafe(DateTime.Now.Add(interval));
				}
				await Task.Delay((int)interval.TotalMilliseconds, stoppingToken);
			}
		}

		private void DoUpdate()
		{
			if (!PreflightSettingsCheck())
				return;

			var updatedAddress = false;
			// if IPv6-only support was not specified, do the IPv4 update
			if (settings.ProtocolSupport != IpSupport.IPv6)
				if (engine.UpdateDnsRecords(IpSupport.IPv4, txtPublicIpv4.Text, txtPublicIpv6.Text, AppendStatusText, DisplayPublicIpAddressThreadSafe))
					updatedAddress = true;

			// if IPv4-only support was not specified, do the IPv6 update
			if (settings.ProtocolSupport != IpSupport.IPv4)
				if (engine.UpdateDnsRecords(IpSupport.IPv6, txtPublicIpv4.Text, txtPublicIpv6.Text, AppendStatusText, DisplayPublicIpAddressThreadSafe))
					updatedAddress = true;

			// fetch and update listview with current status of records if necessary
			if (updatedAddress)
				FetchDsnEntries();
		}


		private void PromptForSettings()
		{
			if (!settings.Validate())
			{
				MessageBox.Show($"Please configure your settings.");
				DisplaySettingsForm();
			}
		}

		private bool PreflightSettingsCheck()
		{
			if (!settings.Validate())
			{
				AppendStatusText($"Settings not configured");
				return false;
			}
			return true;
		}

		private void Init()
		{
			InitNotifyIcon();

			settings = new Settings();
			engine = new Engine(settings);

			if (settings.StartMinimized)
			{
				isInitialMinimize = true;
				WindowState = WindowState.Minimized;
			}

			SetProtocolUiEnabled();
		}

		private void SetProtocolUiEnabled()
		{
			lblPublicIpv4Address.IsEnabled = settings.ProtocolSupport != IpSupport.IPv6;
			txtPublicIpv4.IsEnabled = settings.ProtocolSupport != IpSupport.IPv6;

			lblPublicIpv6Address.IsEnabled = settings.ProtocolSupport != IpSupport.IPv4;
			txtPublicIpv6.IsEnabled = settings.ProtocolSupport != IpSupport.IPv4;
		}

		private void btnUpdateList_Click(object sender, RoutedEventArgs e)
		{
			FetchDsnEntries();
		}

		private void FetchDsnEntries()
		{
			if (!PreflightSettingsCheck())
				return;

			try
			{
				var allDnsRecordsByZone = engine.CloudflareAPI.GetAllDnsRecordsByZone();
				UpdateDnsEntryListUI(allDnsRecordsByZone);
			}
			catch (Exception e)
			{
				AppendStatusText($"Error fetching list: {e.Message}");
				if (settings.IsUsingToken && e.Message.Contains("403 (Forbidden)"))
					AppendStatusText($"Make sure your token has Zone:Read permissions. See https://dash.cloudflare.com/profile/api-tokens to configure.");
			}
		}

		private void UpdateDnsEntryListUI(List<Dns.Result> allDnsRecords)
		{
			Dispatcher.BeginInvoke(new Action(() => UpdateDataGridThreadSafe(allDnsRecords)), System.Windows.Threading.DispatcherPriority.Background, null);
		}

		private void UpdateDataGridThreadSafe(List<Dns.Result> allDnsRecords)
		{
			// TODO: group items by zone (see below)
			observableDnsEntryCollection = new ObservableCollection<DnsEntryViewItem>();
			foreach (var dnsRecord in allDnsRecords)
			{
				var item = new DnsEntryViewItem(settings);
				item.UpdateCloudflare = settings.SelectedDomains
					.Any(entry => entry.ZoneName == dnsRecord.zone_name && entry.DnsName == dnsRecord.name && entry.Type == dnsRecord.type);
				item.DnsName = dnsRecord.name;
				item.Type = dnsRecord.type;
				item.Address = dnsRecord.content;
				item.TTL = dnsRecord.ttl == 1 ? "Auto" : dnsRecord.ttl.ToString();
				item.Proxied = dnsRecord.proxied ? "Yes" : "No";
				item.ZoneName = dnsRecord.zone_name;

				observableDnsEntryCollection.Add(item);
			}

			dgDnsRecords.ItemsSource = observableDnsEntryCollection;

			//// group each zones records under a separate header
			//foreach (var zone in allDnsRecords.Select(d => d.zone_name).Distinct().OrderBy(z => z))
			//{
			//	var group = new ListViewGroup("Zone: " + zone);
			//	listViewRecords.Groups.Add(group);
			//	var zoneDnsRecords = allDnsRecords.Where(d => d.zone_name == zone);
			//	foreach (var dnsRecord in zoneDnsRecords)
			//	{
			//		var row = new ListViewItem(group);
			//		row.SubItems.Add(dnsRecord.type);
			//		row.SubItems.Add(dnsRecord.name);
			//		row.SubItems.Add(dnsRecord.content);
			//		row.SubItems.Add(dnsRecord.proxied ? "Yes" : "No");
			//		row.Tag = zone;

			//		if (settings.SelectedDomains.Any(entry =>
			//				entry.ZoneName == dnsRecord.zone_name
			//				&& entry.DnsName == dnsRecord.name
			//				&& entry.Type == dnsRecord.type))
			//			row.Checked = true;

			//		listViewRecords.Items.Add(row);
			//	}
			//}
		}

		private void btnQuit_Click(object sender, RoutedEventArgs e)
		{
			Environment.Exit(0);
		}

		private void btnSettings_Click(object sender, RoutedEventArgs e)
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
			engine.Settings = settings;

			SetProtocolUiEnabled();

			// pick up new credentials if they were changed
			engine.InitCloudflareClient(settings);
			// pick up new interval if it was changed
			cancellationTokenSource = new CancellationTokenSource();

			ScheduleUpdates(cancellationTokenSource.Token);
		}

		private void AppendStatusTextThreadSafe(string s)
		{
			txtOutput.Text += $"{Core.Utility.GetDateString()}: {s}\r\n";
		}

		private void AppendStatusText(string s)
		{
			Dispatcher.BeginInvoke(new Action(() => AppendStatusTextThreadSafe(s)), System.Windows.Threading.DispatcherPriority.Background, null);
		}

		private void DisplayPublicIpAddressThreadSafe(IpSupport protocol, string s)
		{
			TextBox input = protocol == IpSupport.IPv4 ? txtPublicIpv4 : txtPublicIpv6;
			input.Text = s;
		}

		private void SetNextUpdateTextThreadSafe(DateTime d)
		{
			txtNextUpdate.Text = d.ToString("h:mm:ss tt");
		}

		private void btnUpdate_Click(object sender, RoutedEventArgs e)
		{
			AppendStatusText($"Manually updating IP address");
			DoUpdate();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			cancellationTokenSource.Cancel();

			base.OnClosing(e);
		}



	}
}
