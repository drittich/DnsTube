using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;
using Microsoft.Extensions.Logging;

namespace DnsTube.Cli.Services
{
	/// <summary>
	/// CLI-specific logging that outputs to console instead of database
	/// </summary>
	public class ConsoleLogService : ILogService
	{
		private readonly IOutputService _output;

		public ConsoleLogService(IOutputService output)
		{
			_output = output;
		}

		public Task WriteAsync(string message, LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Error:
				case LogLevel.Critical:
					_output.WriteError(message);
					break;
				case LogLevel.Warning:
					_output.WriteWarning(message);
					break;
				case LogLevel.Information:
					_output.WriteInfo(message);
					break;
				case LogLevel.Debug:
				case LogLevel.Trace:
					// Only show in verbose mode (handled by logger configuration)
					_output.WriteInfo(message);
					break;
			}

			return Task.CompletedTask;
		}

		public Task<List<LogEntry>> GetAsync(int? pageSize = 10, int? lastLogId = null)
		{
			// History not available in CLI mode
			return Task.FromResult(new List<LogEntry>());
		}

		public Task ClearAsync()
		{
			// No-op for CLI
			return Task.CompletedTask;
		}
	}
}