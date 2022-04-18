using System;

using DnsTube.Services;

using NUnit.Framework;

namespace DnsTube.Tests
{
	public class Tests
	{
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void CanReplaceIp4InTxtRecord()
		{
			var spfRecord = @"v=spf1 ip4:192.168.10.1 ~all";
			var newAddress = @"192.168.10.2";
			var spfRecord2 = DnsService.UpdateDnsRecordContent(IpSupport.IPv4, spfRecord, newAddress);
			Assert.AreEqual(spfRecord2, @"v=spf1 ip4:192.168.10.2 ~all");
		}

		[Test]
		public void CanReplaceIp6InTxtRecord()
		{
			var spfRecord = @"v=spf1 ip6:1080::8:800:0000:0000 ~all";
			var newAddress = @"1080::8:800:0000:0001";
			var spfRecord2 = DnsService.UpdateDnsRecordContent(IpSupport.IPv6, spfRecord, newAddress);
			Assert.AreEqual(spfRecord2, @"v=spf1 ip6:1080::8:800:0000:0001 ~all");
		}

		[Test]
		public void CanReplaceIp4AndIp6InTxtRecord()
		{
			var spfRecord = @"v=spf1 ip4:192.168.10.1 ip6:1080::8:800:0000:0000 ~all";
			var newIp4Address = @"192.168.10.2";
			var newIp6Address = @"1080::8:800:0000:0001";
			var spfRecord2 = DnsService.UpdateDnsRecordContent(IpSupport.IPv4, spfRecord, newIp4Address);
			spfRecord2 = DnsService.UpdateDnsRecordContent(IpSupport.IPv6, spfRecord2, newIp6Address);
			Assert.AreEqual(spfRecord2, @"v=spf1 ip4:192.168.10.2 ip6:1080::8:800:0000:0001 ~all");
		}
	}
}