using System;

namespace DnsTube.Dns
{
	public class DnsRecordsResponse
	{
		public bool success { get; set; }
		public object[] errors { get; set; }
		public object[] messages { get; set; }
		public Result[] result { get; set; }
		public Result_Info result_info { get; set; }
	}

	public class Result_Info
	{
		public int page { get; set; }
		public int per_page { get; set; }
		public int count { get; set; }
		public int total_count { get; set; }
	}

	public class Result
	{
		public string id { get; set; }
		public string type { get; set; }
		public string name { get; set; }
		public string content { get; set; }
		public bool proxiable { get; set; }
		public bool proxied { get; set; }
		public int ttl { get; set; }
		public bool locked { get; set; }
		public string zone_id { get; set; }
		public string zone_name { get; set; }
		public DateTime created_on { get; set; }
		public DateTime modified_on { get; set; }
		public Data data { get; set; }
	}

	public class Data
	{
	}

	public class DnsUpdateResponse
	{
		public bool success { get; set; }
		public object[] errors { get; set; }
		public object[] messages { get; set; }
		public Result result { get; set; }
	}

	public class DnsUpdateRequest
	{
		public string type { get; set; }
		public string name { get; set; }
		public string content { get; set; }
		public bool proxied { get; set; }
	}
}
