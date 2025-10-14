using DnsTube.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DnsTube.Cli.Services
{
	/// <summary>
	/// File-based settings service for standalone mode (no database)
	/// </summary>
	public class FileSettingsService : ISettingsService
	{
		private readonly ILogger<FileSettingsService> _logger;
		private readonly IConfigFileService _configFileService;
		private ISettings? _currentSettings;

		public FileSettingsService(ILogger<FileSettingsService> logger, IConfigFileService configFileService)
		{
			_logger = logger;
			_configFileService = configFileService;
		}

		public async Task<ISettings> GetAsync(bool forceRefetch = false)
		{
			if (_currentSettings is null || forceRefetch)
			{
				if (string.IsNullOrEmpty(_configFileService.ConfigFilePath))
				{
					throw new InvalidOperationException("Config file path not set in standalone mode");
				}

				_currentSettings = await _configFileService.LoadFromFileAsync(_configFileService.ConfigFilePath);
			}

			return _currentSettings;
		}

		public async Task SaveAsync(ISettings settings)
		{
			if (string.IsNullOrEmpty(_configFileService.ConfigFilePath))
			{
				throw new InvalidOperationException("Config file path not set in standalone mode");
			}

			await _configFileService.SaveToFileAsync(_configFileService.ConfigFilePath, settings);
			_currentSettings = settings;
		}

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

		public Task SaveDomainsAsync(IList<Core.Models.SelectedDomain> domains)
		{
			throw new NotImplementedException("Use GetAsync() then SaveAsync() in standalone mode");
		}

		public Task RemoveAllSelectedDomainsAsync()
		{
			throw new NotImplementedException("Use GetAsync() then SaveAsync() in standalone mode");
		}

		public void ClearCache()
		{
			_currentSettings = null;
		}
	}
}