namespace DnsTube.Core.Models
{
	public class SelectedDomain
	{
		public string ZoneName { get; set; }
		public string DnsName { get; set; }
		public string Type { get; set; }
		public string? NetworkAdapterName { get; set; }
	}
}
