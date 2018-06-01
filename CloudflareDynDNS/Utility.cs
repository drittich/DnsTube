using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudflareDynDNS
{
	public class Utility
	{
		public static void SaveSetting(string key, string value)
		{
			Properties.Settings.Default[key] = value;
			Properties.Settings.Default.Save();

		}

		public static string GetSetting(string key)
		{
			return Properties.Settings.Default[key]?.ToString();
		}
	}
}
