using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
		}

		public string GetDbFolder()
		{
			if (_dbFolder is null)
			{
				Environment.SpecialFolder rootFolder;

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

		public async Task<SqliteConnection> GetConnectionAsync()
		{
			var dbFolder = GetDbFolder();
			string dbpath = Path.Combine(dbFolder, "DnsTube.db");

			var cn = new SqliteConnection($"Data Source={dbpath};");
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
