namespace DnsTube.Core.Models
{
	internal class LogEntryDTO
	{
		public int Id { get; set; }
		public int Created { get; set; }
		public string Text { get; set; } = string.Empty;
		public int LogLevel { get; set; }
	}
}
