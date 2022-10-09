using System;
using System.Text.RegularExpressions;

namespace DnsTube.Services
{
	public class DnsService
	{
		const string Ipv6DnsRegex = @"(?<prefix>.*)(?<key>ip6:)(?<value>[0-9a-f:]+)(?<suffix>.*)";

		public static string UpdateDnsRecordContent(IpSupport protocol, string content, string publicIpAddress)
		{
			// we need explicit protocol in this method
			if (protocol == IpSupport.IPv4AndIPv6)
				throw new ArgumentOutOfRangeException();

			var newContent = content;

			if (protocol == IpSupport.IPv4)
				newContent = Regex.Replace(newContent, Utility.Ipv4Regex, publicIpAddress);
			else if (protocol == IpSupport.IPv6)
				newContent = Regex.Replace(newContent, Ipv6DnsRegex, $"${{prefix}}${{key}}{publicIpAddress}${{suffix}}");

			return newContent;
		}
	}
}
