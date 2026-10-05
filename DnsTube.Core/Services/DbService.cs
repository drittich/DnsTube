using System.Diagnostics;

using Dapper;

using DnsTube.Core.Interfaces;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Services
{
	public class DbService : IDbService
	{
		/// <summary>
		/// Environment variable that, when set, overrides the folder holding the database.
		/// </summary>
		public const string DbFolderEnvironmentVariable = "DNSTUBE_DB_FOLDER";

		private ILogger<DbService> _logger;
		private string? _dbFolder;

		public DbService(ILogger<DbService> logger)
		{
			_logger = logger;

			Task.Run(() => EnableWalAsync().Wait());
		}

		private async Task EnableWalAsync()
		{
			using (var cn = await GetConnectionAsync())
			{
				await cn.ExecuteAsync("PRAGMA journal_mode=WAL;");
			}
		}

		public string GetDbFolder()
		{
			if (_dbFolder is null)
			{
				string dbFolder;

				// an explicit override isolates the database, e.g. for in-process tests
				var overrideFolder = Environment.GetEnvironmentVariable(DbFolderEnvironmentVariable);
				if (!string.IsNullOrWhiteSpace(overrideFolder))
				{
					dbFolder = overrideFolder;
				}
				else
				{
					Environment.SpecialFolder rootFolder;

					// use a separate folder for the database if we're developing
					if (Debugger.IsAttached)
						rootFolder = Environment.SpecialFolder.LocalApplicationData;
					else
						rootFolder = Environment.SpecialFolder.CommonApplicationData;

					dbFolder = Path.Combine(Environment.GetFolderPath(rootFolder), "DnsTube");
				}

				_logger.LogInformation($"Db folder: {dbFolder}");

				// create the folder before caching it: the constructor's WAL task and the
				// first caller can race here, and a cached path must already exist
				Directory.CreateDirectory(dbFolder);
				_dbFolder = dbFolder;
			}
			return _dbFolder;
		}

		public string GetDbPath()
		{
			var dbFolder = GetDbFolder();
			string dbPath = Path.Combine(dbFolder, "DnsTube.db");
			return dbPath;
		}

		public async Task<SqliteConnection> GetConnectionAsync()
		{
			var cn = new SqliteConnection($"Data Source={GetDbPath()};");
			await cn.OpenAsync();

			return cn;
		}

		public long DateTimeToUnixSeconds(DateTime date)
		{
			return (long)(date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
		}

		public DateTime UnixSecondsToDateTime(long seconds)
		{
			return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds);
		}
	}
}
