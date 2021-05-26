using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsTube2
{
	public class DnsEntryViewItem: INotifyPropertyChanged
	{
		private bool _update;

		public bool Update
		{
			get { return _update; }
			set
			{
				_update = value;
				OnPropertyChanged("Update");
			}
		}
		public string Name { get; set; }
		public string Type { get; set; }
		public string Address { get; set; }
		public string Proxied { get; set; }
		public string Zone { get; set; }

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
