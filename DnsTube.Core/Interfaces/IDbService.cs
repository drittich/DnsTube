using Microsoft.Data.Sqlite;

namespace DnsTube.Core.Interfaces
{
	public interface IDbService
	{
		public string GetDbFolder();
		public string GetDbPath();

		public Task<SqliteConnection> GetConnectionAsync();

		public long DateTimeToUnixSeconds(DateTime date);
		
		public DateTime UnixSecondsToDateTime(long seconds);
	}
}
