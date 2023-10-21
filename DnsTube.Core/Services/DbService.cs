﻿using System.Diagnostics;

using Dapper;

using DnsTube.Core.Interfaces;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Services
{
	public class DbService : IDbService
	{
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
				Environment.SpecialFolder rootFolder;

				// use a separate folder for the database if we're developing
				if (Debugger.IsAttached)
					rootFolder = Environment.SpecialFolder.LocalApplicationData;
				else
					rootFolder = Environment.SpecialFolder.CommonApplicationData;

				_dbFolder = Path.Combine(Environment.GetFolderPath(rootFolder), "DnsTube");
				_logger.LogInformation($"Db folder: {_dbFolder}");

				Directory.CreateDirectory(_dbFolder);
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
