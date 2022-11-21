using System.Diagnostics;

using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;

using Lib.AspNetCore.ServerSentEvents;

//using Newtonsoft.Json;

namespace DnsTube.Service
{
	public class WorkerService : BackgroundService
	{
		private readonly ILogger<WorkerService> _logger;
		private ISettingsService _settingsService;
		private IGitHubService _githubService;
		private ICloudflareService _cloudflareService;
		private ILogService _logService;
		private IIpAddressService _ipAddressService;
		private IConfiguration _configuration;
		private IServerSentEventsService _serverSentEventsService;

		public WorkerService(ILogger<WorkerService> logger, ISettingsService settingsService, IGitHubService githubService, ICloudflareService cloudflareService, ILogService logService, IIpAddressService ipAddressService, IConfiguration configuration, IServerSentEventsService serverSentEventsService)
		{
			_logger = logger;
			_settingsService = settingsService;
			_githubService = githubService;
			_cloudflareService = cloudflareService;
			_logService = logService;
			_ipAddressService = ipAddressService;
			_configuration = configuration;
			_serverSentEventsService = serverSentEventsService;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// log DnsTube version
			var version = $"Running DnsTube {Application.RELEASE_TAG}";
			_logger.LogInformation(version);
			await _logService.WriteAsync(version, LogLevel.Information);

			_logger.LogInformation($"UI hosted at {_configuration["Url"]}");

			// log new release info
			var latestReleaseTag = await _githubService.GetLatestReleaseTagNameAsync();
			if (latestReleaseTag != Application.RELEASE_TAG)
			{
				var msg = "You are not running the latest release. See https://github.com/drittich/DnsTube/releases/latest for more information.";
				_logger.LogInformation(msg);
				await _logService.WriteAsync(msg, LogLevel.Information);
			}

			string? previousIpv4Address = null;
			string? previousIpv6Address = null;

			while (!stoppingToken.IsCancellationRequested)
			{
				var msg = $"Worker running at: {DateTimeOffset.Now}";
				_logger.LogTrace(msg);

				var settings = await _settingsService.GetAsync(true);
				string? currentPublicIpv4Address = null;
				string? currentPublicIpv6Address = null;

				currentPublicIpv4Address = await GetIpAddressAsync(IpSupport.IPv4, previousIpv4Address, settings);
				currentPublicIpv6Address = await GetIpAddressAsync(IpSupport.IPv6, previousIpv6Address, settings);
				previousIpv4Address = currentPublicIpv4Address;
				previousIpv6Address = currentPublicIpv6Address;

				var validationErrorMessage = _settingsService.ValidateSettings(settings);

				if (validationErrorMessage is not null)
				{
					if (validationErrorMessage == "No selected domains")
					{
						_logger.LogWarning($"{validationErrorMessage}, go to {_configuration["Url"]} to update");
						await _logService.WriteAsync($"{validationErrorMessage}, select the DNS entries you want to update above", LogLevel.Warning);
					}
					else
					{
						_logger.LogWarning($"{validationErrorMessage}, go to {_configuration["Url"]}/settings.html to update");
						await _logService.WriteAsync($"{validationErrorMessage}, go to the <a href=\"{_configuration["Url"]}/settings.html\">Settings</a> tab to update", LogLevel.Warning);
					}
				}
				else
				{
					var selectedDomainsValid = await _cloudflareService.ValidateSelectedDomainsAsync();
					if (selectedDomainsValid)
						await DoUpdateAsync(currentPublicIpv4Address, currentPublicIpv6Address);
				}

				var intervalMs = Debugger.IsAttached ? 30000 : settings.UpdateIntervalMinutes * 60 * 1000;
				var nextUpdate = DateTime.Now.AddMilliseconds(intervalMs);

				await _serverSentEventsService.SendEventAsync(new ServerSentEvent
				{
					Type = "next-update",
					Data = new List<string> { $"{nextUpdate.ToString("yyyy-MM-ddTHH:mm:ss")}" }
				});

				await Task.Delay(intervalMs, stoppingToken);
			}
		}

		private async Task<string> GetIpAddressAsync(IpSupport ipSupport, string? previousPublicAddress, ISettings settings)
		{
			var exclusionValue = ipSupport == IpSupport.IPv4 ? IpSupport.IPv6 : IpSupport.IPv4;
			string? currentPublicAddress = null;

			if (settings.ProtocolSupport != exclusionValue)
			{
				currentPublicAddress = await _ipAddressService.GetPublicIpAddressAsync(ipSupport);
				if (currentPublicAddress != previousPublicAddress)
				{
					string msg;
					if (previousPublicAddress is null)
						msg = $"Current public {ipSupport} address: {currentPublicAddress}";
					else
						msg = $"Public {ipSupport} address changed from {previousPublicAddress} to {currentPublicAddress}";

					_logger.LogInformation(msg);
					await _logService.WriteAsync(msg, LogLevel.Information);
				}
			}

			return currentPublicAddress;
		}

		private async Task DoUpdateAsync(string? publicIpv4Address, string? publicIpv6Address)
		{
			var updatedAddress = false;
			var settings = await _settingsService.GetAsync();
			// if IPv6-only support was not specified, do the IPv4 update
			if (settings.ProtocolSupport != IpSupport.IPv6)
				if (await _cloudflareService.UpdateDnsRecordsAsync(IpSupport.IPv4, publicIpv4Address))
					updatedAddress = true;

			// if IPv4-only support was not specified, do the IPv6 update
			if (settings.ProtocolSupport != IpSupport.IPv4)
				if (await _cloudflareService.UpdateDnsRecordsAsync(IpSupport.IPv6, publicIpv6Address))
					updatedAddress = true;

			// fetch and update listview with current status of records if necessary
			//TODO: make web ui update records listview
			//if (updatedAddress)
			//	await FetchDsnEntriesAsync();
		}


		//private void FetchDsnEntries()
		//{
		//	if (!PreflightSettingsCheck())
		//		return;

		//	try
		//	{
		//		var allDnsRecordsByZone = _engine.CloudflareApi.GetAllDnsRecordsByZone();
		//		//FIXME
		//		//LogDnsEntriesToUpdate(allDnsRecordsByZone);
		//	}
		//	catch (Exception e)
		//	{
		//		_logger.LogError($"Error fetching list: {e.Message}");
		//		if (_settings.IsUsingToken && e.Message.Contains("403 (Forbidden)"))
		//			_logger.LogError($"Make sure your token has Zone:Read permissions. See https://dash.cloudflare.com/profile/api-tokens to configure.");
		//	}
		//}
	}
}
