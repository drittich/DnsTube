using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsTube.Core.Models
{
	public class DnsEntry
	{
		public bool? UpdateCloudflare { get; set; }
		public string? DnsName { get; set; }
		public string? Type { get; set; }
		public string? Address { get; set; }
		public int? TTL { get; set; }
		public bool? Proxied { get; set; }
		public string? ZoneName { get; set; }
	}
}
