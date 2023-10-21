namespace DnsTube.Core.Models
{
	public class TableColumnInfo
	{
		public int cid { get; set; }
		public required string name { get; set; }
		public required string type { get; set; }
		public int notnull { get; set; }
		public required string dflt_value { get; set; }
		public int pk { get; set; }

		public TableColumnInfo() { }
	}
}
