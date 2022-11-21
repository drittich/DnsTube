using Dapper;

using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;

using Lib.AspNetCore.ServerSentEvents;

using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Services
{
	public class LogService : ILogService
	{
		private ILogger<LogService> _logger;
		private IDbService _dbService;
		private IServerSentEventsService _serverSentEventsService;

		public LogService(ILogger<LogService> logger, IDbService dbService, IServerSentEventsService serverSentEventsService)
		{
			_logger = logger;
			_dbService = dbService;
			_serverSentEventsService = serverSentEventsService;

			InitAsync().Wait();
		}

		private async Task InitAsync()
		{
			using (var cn = await _dbService.GetConnectionAsync())
			{
				await cn.ExecuteAsync($@"
					CREATE TABLE IF NOT EXISTS Log (
						Id INTEGER PRIMARY KEY AUTOINCREMENT,
						Text TEXT NOT NULL,
						LogLevel INTEGER NOT NULL,
						Created INTEGER NOT NULL
					);
					CREATE INDEX IF NOT EXISTS IX_Created_Id ON Log (Created desc, Id desc); 
				");
			}
		}

		public async Task WriteAsync(string text, LogLevel logLevel)
		{
			var now = _dbService.DateTimeToUnixSeconds(DateTime.UtcNow);
			var sql = $@"
				insert into Log 
				(Text, LogLevel, Created) values 
				(@text, @logLevel, @Created);";
			var data = new
			{
				text,
				logLevel = (int)logLevel,
				Created = now
			};

			using (var cn = await _dbService.GetConnectionAsync())
			{
				await cn.ExecuteAsync(sql, data);
			}

			await _serverSentEventsService.SendEventAsync(new ServerSentEvent
			{
				Type = "log-updated"
			});
		}

		public async Task<List<LogEntry>> GetAsync(int? pageSize = 10, int? lastLogId = null)
		{
			string where = "";

			if (lastLogId is not null)
				where = $"where Id < @lastLogId";

			var sql = $@"
				select Id, Text, LogLevel, Created
				from Log
				{where}
				order by Created desc, Id desc
				limit (@pageSize);";

			using (var cn = await _dbService.GetConnectionAsync())
			{
				var logEntryDTOs = await cn.QueryAsync<LogEntryDTO>(sql, new { lastLogId, pageSize });
				return logEntryDTOs.Select(le => new LogEntry(le, _dbService)).ToList();
			}
		}

		public async Task ClearAsync()
		{

			var sql = $@"
				delete from Log;
				vacuum;";

			using (var cn = await _dbService.GetConnectionAsync())
			{
				await cn.ExecuteAsync(sql);
			}
		}
	}
}
