using System.ComponentModel;
using System.Linq;

namespace DnsTube
{
	public class DnsEntryViewItem: INotifyPropertyChanged
	{
		private bool _update;
		private Settings _settings;

		public bool UpdateCloudflare
		{
			get { return _update; }
			set
			{
				_update = value;
				OnPropertyChanged("Update");

				if (!_update)
				{
					// Make sure to clean up any old entries in the settings
					_settings.SelectedDomains.RemoveAll(entry => entry.ZoneName == ZoneName && entry.DnsName == DnsName && entry.Type == Type);
					_settings.Save();
				}
				else
				{
					// Item has been selected by the user, store it for later
					if (_settings.SelectedDomains.Any(entry => entry.ZoneName == ZoneName && entry.DnsName == DnsName && entry.Type == Type))
					{
						// Item is already in the settings list, do nothing.
						return;
					}
					_settings.SelectedDomains.Add(new SelectedDomain() { ZoneName = ZoneName, DnsName = DnsName, Type = Type });
					_settings.Save();
				}
			}
		}
		public string DnsName { get; set; }
		public string Type { get; set; }
		public string Address { get; set; }
		public string Proxied { get; set; }
		public string ZoneName { get; set; }

		public DnsEntryViewItem(Settings settings)
		{
			_settings = settings;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			var propertyChanged = PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
