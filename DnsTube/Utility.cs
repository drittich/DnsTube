using System;
using System.Linq;
using System.Net.Http;

namespace DnsTube
{
	public class Utility
	{
		public static string GetPublicIpAddress(HttpClient Client)
		{
			var ret = Client.GetStringAsync("http://icanhazip.com").Result.Replace("\n", "");

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
