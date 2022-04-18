using System;
using System.Text.RegularExpressions;

namespace DnsTube.Services
{
	public class DnsService
	{
		readonly static string ipv4Regex = @"\b(?:(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9]?[0-9])\b";
		readonly static string ipv6Regex = @"(?<prefix>.*)(?<key>ip6:)(?<value>[0-9a-f:]+)(?<suffix>.*)";

		public static string UpdateDnsRecordContent(IpSupport protocol, string content, string publicIpAddress)
		{
			// we need explicit protocol in this method
			if (protocol == IpSupport.IPv4AndIPv6)
				throw new ArgumentOutOfRangeException();

			var newContent = content;

			if (protocol == IpSupport.IPv4)
				newContent = Regex.Replace(newContent, ipv4Regex, publicIpAddress);
			else if (protocol == IpSupport.IPv6)
				newContent = Regex.Replace(newContent, ipv6Regex, $"${{prefix}}${{key}}{publicIpAddress}${{suffix}}");

			return newContent;
		}
	}
}
