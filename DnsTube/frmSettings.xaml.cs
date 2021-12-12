using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Media;

using DnsTube.Core;

namespace DnsTube.Gui
{
	/// <summary>
	/// Interaction logic for frmSettings.xaml
	/// </summary>
	public partial class frmSettings : Window
	{
		Settings settings;

		public frmSettings(Settings settings, ImageSource icon)
		{
			this.settings = settings;
			InitializeComponent();
			Icon = icon;
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Button_Save_Click(object sender, RoutedEventArgs e)
		{
			settings.EmailAddress = txtEmail.Text;
			settings.IsUsingToken = rbUseApiToken.IsChecked.GetValueOrDefault();
			settings.ApiKey = txtApiKey.Text;
			settings.ApiToken = txtApiToken.Text;
			settings.UpdateIntervalMinutes = int.Parse(txtUpdateInterval.Text);
			settings.StartMinimized = chkStartMinimized.IsChecked.GetValueOrDefault();
			settings.MinimizeToTray = chkMinimizeToTray.IsChecked.GetValueOrDefault();
			settings.SkipCheckForNewReleases = !chkNotifyOfUpdates.IsChecked.GetValueOrDefault();
			settings.ProtocolSupport = GetProtocol();
			settings.ZoneIDs = txtZoneIDs.Text;
			settings.IPv4_API = txtIPv4API.Text;
			settings.IPv6_API = txtIPv6API.Text;
			settings.Save();
			Close();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			txtEmail.Text = settings.EmailAddress;
			rbUseApiToken.IsChecked = settings.IsUsingToken;
			rbUseApiKey.IsChecked = !settings.IsUsingToken;
			txtApiKey.Text = settings.ApiKey;
			txtApiToken.Text = settings.ApiToken;
			txtUpdateInterval.Text = settings.UpdateIntervalMinutes.ToString();
			chkStartMinimized.IsChecked = settings.StartMinimized;
			chkMinimizeToTray.IsChecked = settings.MinimizeToTray;
			chkNotifyOfUpdates.IsChecked = !settings.SkipCheckForNewReleases;
			rbProtocolIPv4.IsChecked = settings.ProtocolSupport == IpSupport.IPv4;
			rbProtocolIPv6.IsChecked = settings.ProtocolSupport == IpSupport.IPv6;
			rbProtocolIPv4AndIPv6.IsChecked = settings.ProtocolSupport == IpSupport.IPv4AndIPv6;
			txtZoneIDs.Text = settings.ZoneIDs;
			txtIPv4API.Text = settings.IPv4_API;
			txtIPv6API.Text = settings.IPv6_API;

			HandleAuthDisplay();
		}

		private IpSupport GetProtocol()
		{
			if (rbProtocolIPv4.IsChecked.GetValueOrDefault())
				return IpSupport.IPv4;
			if (rbProtocolIPv6.IsChecked.GetValueOrDefault())
				return IpSupport.IPv6;
			return IpSupport.IPv4AndIPv6;
		}

		private void HandleAuthDisplay()
		{
			if (rbUseApiKey.IsChecked.GetValueOrDefault())
			{
				txtApiToken.Visibility = Visibility.Collapsed;
				lblApiToken.Visibility = Visibility.Collapsed;
				txtApiKey.Visibility = Visibility.Visible;
				lblApiKey.Visibility = Visibility.Visible;

			}
			if (rbUseApiToken.IsChecked.GetValueOrDefault())
			{
				txtApiToken.Visibility = Visibility.Visible;
				lblApiToken.Visibility = Visibility.Visible;
				txtApiKey.Visibility = Visibility.Collapsed;
				lblApiKey.Visibility = Visibility.Collapsed;
			}
		}

		private void txtUpdateInterval_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			bool isNumPadNumeric = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9);
			bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9);
			bool isBackspaceOrDelete = (e.Key >= Key.Back && e.Key <= Key.Delete);
			bool isHomeOrEnd = (e.Key >= Key.Home && e.Key <= Key.End);

			if (!isNumPadNumeric && !isNumeric && !isBackspaceOrDelete && !isHomeOrEnd)
			{
				e.Handled = true;
			}
		}

		private void rbUseApiKey_Checked(object sender, RoutedEventArgs e)
		{
			settings.IsUsingToken = rbUseApiToken.IsChecked.GetValueOrDefault();
			HandleAuthDisplay();
		}

		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			// for .NET Core you need to add UseShellExecute = true
			// see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
			e.Handled = true;
		}
	}
}
