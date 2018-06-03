using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CloudflareDynDNS
{
	public class Utility
	{
		public static string GetExternalAddress(HttpClient Client)
		{
			var ret = Client.GetStringAsync("http://icanhazip.com").Result;

			if (!ValidateIPv4(ret))
				return null;

			return ret;
		}

		public static string GetDateString()
		{
			return DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");
		}

		public static bool ValidateIPv4(string ipString)
		{
			if (String.IsNullOrWhiteSpace(ipString))
				return false;

			string[] splitValues = ipString.Split('.');
			if (splitValues.Length != 4)
				return false;

			byte tempForParsing;
			return splitValues.All(r => byte.TryParse(r, out tempForParsing));
		}
	}
}
