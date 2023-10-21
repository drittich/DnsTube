using System.Text.Json;

using Dapper;

using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;

using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Services
{
	public class SettingsService : ISettingsService
	{
		private readonly ILogger<SettingsService> _logger;
		private readonly IDbService _dbService;
		private ISettings? _currentSettings;

		public SettingsService(ILogger<SettingsService> logger, IDbService dbService)
		{
			_logger = logger;
			_dbService = dbService;

			InitAsync().Wait();
		}

		private async Task InitAsync()
		{
			using (var cn = await _dbService.GetConnectionAsync())
			{
				// create Settings table for new installs
				await cn.ExecuteAsync($@"
					CREATE TABLE IF NOT EXISTS Settings (
						ApiKeyOrToken TEXT,
						EmailAddress TEXT,
						IPv4_API TEXT,
						IPv6_API TEXT,
						IsUsingToken BOOLEAN NOT NULL CHECK (IsUsingToken IN (0, 1)),
						ProtocolSupport INTEGER NOT NULL CHECK (ProtocolSupport IN (0, 1, 2)),
						PublicIpv4Address TEXT,
						PublicIpv6Address TEXT,
						SelectedDomains TEXT,
						SkipCheckForNewReleases BOOLEAN NOT NULL CHECK (SkipCheckForNewReleases IN (0, 1)),
						UpdateIntervalMinutes INTEGER,
						ZoneIDs TEXT,
						NetworkAdapter TEXT
					);
				");

				// migrate older installs
				var tableColumns = await cn.QueryAsync<TableColumnInfo>(@"PRAGMA table_info(Settings);");
				if (!tableColumns.Any(c => c.name == "NetworkAdapter"))
				{
					_logger.LogInformation("Migrating Settings table");
					await cn.ExecuteAsync($@"
						ALTER TABLE Settings
						ADD COLUMN NetworkAdapter TEXT;
					");
				}
			}
		}

		public async Task<ISettings> GetAsync(bool forceRefetch = false)
		{
			if (_currentSettings is null || forceRefetch)
			{
				string sql = $@"
					select ApiKeyOrToken,
						EmailAddress,
						IPv4_API,
						IPv6_API,
						IsUsingToken,
						ProtocolSupport,
						PublicIpv4Address,
						PublicIpv6Address,
						SelectedDomains,
						SkipCheckForNewReleases,
						UpdateIntervalMinutes,
						ZoneIDs,
						NetworkAdapter
					from Settings;";

				using (var cn = await _dbService.GetConnectionAsync())
				{
					var settingsDTO = await cn.QuerySingleOrDefaultAsync<SettingsDTO>(sql);
					if (settingsDTO is null)
					{
						var settings = new Settings();
						settings.UpdateIntervalMinutes = 30;
						settings.SelectedDomains = new List<SelectedDomain>();
						settings.IsUsingToken = true;
						settings.ProtocolSupport = IpSupport.IPv4;
						settings.ZoneIDs = string.Empty;
						settings.IPv4_API = "https://api.ipify.org/";
						settings.IPv6_API = "https://api64.ipify.org/";
						settings.NetworkAdapter = string.Empty;

						await SaveAsync(settings);
						_currentSettings = settings;
					}
					else
					{
						_currentSettings = new Settings(settingsDTO);
					}
				}
			}

			return _currentSettings!;
		}

		public async Task SaveAsync(ISettings settings)
		{
			using (var cn = await _dbService.GetConnectionAsync())
			{
				using (var txn = await cn.BeginTransactionAsync())
				{
					var parms = new
					{
						ApiKeyOrToken = settings.ApiKeyOrToken,
						EmailAddress = settings.EmailAddress,
						IPv4_API = settings.IPv4_API,
						IPv6_API = settings.IPv6_API,
						IsUsingToken = settings.IsUsingToken ? 1 : 0,
						ProtocolSupport = (int)settings.ProtocolSupport,
						PublicIpv4Address = settings.PublicIpv4Address,
						PublicIpv6Address = settings.PublicIpv6Address,
						SelectedDomains = JsonSerializer.Serialize(settings.SelectedDomains),
						SkipCheckForNewReleases = settings.SkipCheckForNewReleases ? 1 : 0,
						UpdateIntervalMinutes = settings.UpdateIntervalMinutes,
						ZoneIDs = settings.ZoneIDs,
						NetworkAdapter = settings.NetworkAdapter
					};
					await cn.ExecuteAsync(@"
						delete from Settings;
						insert into Settings
						(
							ApiKeyOrToken,
							EmailAddress,
							IPv4_API,
							IPv6_API,
							IsUsingToken,
							ProtocolSupport,
							PublicIpv4Address,
							PublicIpv6Address,
							SelectedDomains,
							SkipCheckForNewReleases,
							UpdateIntervalMinutes,
							ZoneIDs,
							NetworkAdapter
						) values
						(
							@ApiKeyOrToken,
							@EmailAddress,
							@IPv4_API,
							@IPv6_API,
							@IsUsingToken,
							@ProtocolSupport,
							@PublicIpv4Address,
							@PublicIpv6Address,
							@SelectedDomains,
							@SkipCheckForNewReleases,
							@UpdateIntervalMinutes,
							@ZoneIDs,
							@NetworkAdapter
						)", parms);
					txn.Commit();
				}
			}
			ClearCache();
		}

		/// <summary>
		/// Validates the settings.
		/// </summary>
		/// <param name="settings"></param>
		/// <returns>
		/// Null if settings are valid, error message otherwise.
		/// </returns>
		public string? ValidateSettings(ISettings settings)
		{
			if (string.IsNullOrWhiteSpace(settings.ApiKeyOrToken))
				return "API key or token not configured";

			if (!settings.EmailAddress.Contains('@'))
				return "Email address not configured";

			if (settings.SelectedDomains.Count == 0)
				return "No selected domains";

			if (settings.UpdateIntervalMinutes == 0)
				return "Update interval not configured";

			return null;
		}

		public async Task SaveDomainsAsync(IList<SelectedDomain> domains)
		{
			var settings = await GetAsync(true);
			var parms = new
			{
				ApiKeyOrToken = settings.ApiKeyOrToken,
				EmailAddress = settings.EmailAddress,
				IPv4_API = settings.IPv4_API,
				IPv6_API = settings.IPv6_API,
				IsUsingToken = settings.IsUsingToken ? 1 : 0,
				ProtocolSupport = (int)settings.ProtocolSupport,
				PublicIpv4Address = settings.PublicIpv4Address,
				PublicIpv6Address = settings.PublicIpv6Address,
				SelectedDomains = JsonSerializer.Serialize(domains),
				SkipCheckForNewReleases = settings.SkipCheckForNewReleases ? 1 : 0,
				UpdateIntervalMinutes = settings.UpdateIntervalMinutes,
				ZoneIDs = settings.ZoneIDs,
				NetworkAdapter = settings.NetworkAdapter
			};

			using (var cn = await _dbService.GetConnectionAsync())
			{
				using (var txn = await cn.BeginTransactionAsync())
				{
					await cn.ExecuteAsync(@"
						delete from Settings;
						insert into Settings
						(
							ApiKeyOrToken,
							EmailAddress,
							IPv4_API,
							IPv6_API,
							IsUsingToken,
							ProtocolSupport,
							PublicIpv4Address,
							PublicIpv6Address,
							SelectedDomains,
							SkipCheckForNewReleases,
							UpdateIntervalMinutes,
							ZoneIDs,
							NetworkAdapter
						) values
						(
							@ApiKeyOrToken,
							@EmailAddress,
							@IPv4_API,
							@IPv6_API,
							@IsUsingToken,
							@ProtocolSupport,
							@PublicIpv4Address,
							@PublicIpv6Address,
							@SelectedDomains,
							@SkipCheckForNewReleases,
							@UpdateIntervalMinutes,
							@ZoneIDs,
							@NetworkAdapter
						)", parms);
					txn.Commit();
				}
			}
			ClearCache();
		}

		public async Task RemoveAllSelectedDomainsAsync()
		{
			var settings = await GetAsync();
			settings.SelectedDomains.Clear();
			await SaveAsync(settings);
		}

		public void ClearCache()
		{
			_currentSettings = null;
		}
	}
}
