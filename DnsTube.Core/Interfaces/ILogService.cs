using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Interfaces
{
	public interface ILogService
	{
		public Task WriteAsync(string text, LogLevel logLevel);
		public Task<List<LogEntry>> GetAsync(int? pageSize = 10, int? lastLogId = null);
		public Task ClearAsync();
	}
}

