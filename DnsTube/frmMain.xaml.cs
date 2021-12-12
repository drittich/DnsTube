﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using DnsTube.Core;

using Hardcodet.Wpf.TaskbarNotification;

namespace DnsTube.Gui
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
			//Icon = BitmapFrame.Create(new Uri("pack://application:,,,/DnsTube.Gui;component/icon-48.ico"));
			bool isFirstInstance;
			_mutex = new Mutex(true, appName, out isFirstInstance);
			if (!isFirstInstance)
			{
				MessageBox.Show("DnsTube is already running", "DnsTube", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterScreen;

			Icon = GetFormIconImageSource();
		}

		/// <summary>
		/// Ref: https://stackoverflow.com/a/65843713/39430
		/// Ref: https://stackoverflow.com/a/6580799/39430
		/// </summary>
		private ImageSource GetFormIconImageSource()
		{
			var assembly = Assembly.GetExecutingAssembly();//reflects the current executable
			var resourceName = $"{GetType().Namespace}.icon-48.ico";

			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
					throw new Exception("Icon resource not found");
				else
					return new Icon(stream).ToImageSource();
			}
		}

		/// <summary>
		/// Ref: https://stackoverflow.com/a/65843713/39430
		/// Ref: https://stackoverflow.com/a/6580799/39430
		/// </summary>
		private Icon GetFormIcon()
		{
			var assembly = Assembly.GetExecutingAssembly();//reflects the current executable
			var resourceName = $"{GetType().Namespace}.icon-48.ico";

			using (var stream = assembly.GetManifestResourceStream(resourceName))
			{
				if (stream == null)
					throw new Exception("Icon resource not found");
				else
					return new Icon(stream);
			}
		}

		private void InitNotifyIcon()
		{
			notifyIcon1 = new TaskbarIcon();
			notifyIcon1.Icon = GetFormIcon();
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
				// Line below to fix a bug where title bar icon was gone after restore from tray 
				Icon = GetFormIconImageSource();
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

			//DisplayAndLogPublicIpAddress();

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

		//public void DisplayAndLogPublicIpAddress()
		//{
		//	string? errorMesssage;
		//	if (settings.ProtocolSupport != IpSupport.IPv6)
		//	{

		//		var publicIpv4Address = engine.GetPublicIpAddress(IpSupport.IPv4, out errorMesssage);
		//		if (publicIpv4Address == null)
		//		{
		//			AppendStatusText($"Error getting public IPv4 address: {errorMesssage}");
		//		}
		//		else
		//		{
		//			AppendStatusText($"Detected public IPv4 address {publicIpv4Address}");
		//			// update UI
		//			if (txtPublicIpv4.Text != publicIpv4Address)
		//				txtPublicIpv4.Text = publicIpv4Address;
		//		}

		//	}
		//	if (settings.ProtocolSupport != IpSupport.IPv4)
		//	{
		//		var publicIpv6Address = engine.GetPublicIpAddress(IpSupport.IPv6, out errorMesssage);
		//		if (publicIpv6Address == null)
		//		{
		//			AppendStatusText($"Error detecting public IPv6 address: {errorMesssage}");
		//		}
		//		else
		//		{
		//			AppendStatusText($"Detected public IPv6 address {publicIpv6Address}");
		//			// update UI
		//			if (txtPublicIpv6.Text != publicIpv6Address)
		//				txtPublicIpv6.Text = publicIpv6Address;
		//		}
		//	}
		//}

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
				if (UpdateDnsRecords(IpSupport.IPv4))
					updatedAddress = true;

			// if IPv4-only support was not specified, do the IPv6 update
			if (settings.ProtocolSupport != IpSupport.IPv4)
				if (UpdateDnsRecords(IpSupport.IPv6))
					updatedAddress = true;

			// fetch and update listview with current status of records if necessary
			if (updatedAddress)
				FetchDsnEntries();
		}

		/// <summary>
		/// Returns true if an update was performed
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public bool UpdateDnsRecords(IpSupport protocol)
		{
			string? errorMesssage; 
			var publicIpAddress = engine.GetPublicIpAddress(protocol, out errorMesssage);
			if (publicIpAddress == null)
			{
				AppendStatusText($"Error getting public {protocol} address: {errorMesssage}");
			}
			else
			{
				AppendStatusText($"Detected public {protocol} address {publicIpAddress}");
				// update UI
				var input = protocol == IpSupport.IPv4 ? txtPublicIpv4 : txtPublicIpv6;
				if (input.Text != publicIpAddress)
					input.Text = publicIpAddress;
			}

			var oldPublicIpAddress = protocol == IpSupport.IPv4 ? settings.PublicIpv4Address : settings.PublicIpv6Address;

			if (publicIpAddress == oldPublicIpAddress)
				return false;

			if (protocol == IpSupport.IPv4)
				settings.PublicIpv4Address = publicIpAddress;
			else
				settings.PublicIpv6Address = publicIpAddress;
			settings.Save();

			if (oldPublicIpAddress != null)
				AppendStatusText($"Public {protocol} changed from {oldPublicIpAddress} to {publicIpAddress}");

			DisplayPublicIpAddressThreadSafe(protocol, publicIpAddress);

			List<string> messages;
			var ret = engine.UpdateDnsRecords(protocol, publicIpAddress, out messages);
			foreach(var message in messages)
				AppendStatusText(message);

			return ret;
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

		private void UpdateDnsEntryListUI(List<DnsTube.Core.Dns.Result> allDnsRecords)
		{
			Dispatcher.BeginInvoke(new Action(() => UpdateDataGridThreadSafe(allDnsRecords)), System.Windows.Threading.DispatcherPriority.Background, null);
		}

		private void UpdateDataGridThreadSafe(List<DnsTube.Core.Dns.Result> allDnsRecords)
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
			var frm = new frmSettings(settings, Icon);
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
