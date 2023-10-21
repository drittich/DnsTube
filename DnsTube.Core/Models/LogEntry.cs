using DnsTube.Core.Models;

using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Interfaces
{
	public class LogEntry
	{
		public int Id { get; set; }
		public DateTime Created { get; set; }
		public string Text { get; set; } = string.Empty;
		public LogLevel LogLevel { get; set; }
		public string LogLevelText { get { return LogLevel.ToString(); } }

		internal LogEntry(LogEntryDTO logEntryDTO, IDbService dbService)
		{
			Id = logEntryDTO.Id;
			Created = dbService.UnixSecondsToDateTime(logEntryDTO.Created);
			Text = logEntryDTO.Text;
			LogLevel = (LogLevel)logEntryDTO.LogLevel;
		}
	}
}
