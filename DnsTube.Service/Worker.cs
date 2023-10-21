﻿using System.Net.NetworkInformation;

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
		private readonly ISettingsService _settingsService;
		private readonly IGitHubService _githubService;
		private readonly ICloudflareService _cloudflareService;
		private readonly ILogService _logService;
		private readonly IIpAddressService _ipAddressService;
		private readonly IConfiguration _configuration;
		private readonly IServerSentEventsService _serverSentEventsService;
		private static bool isManualUpdate = false;

		public static DateTimeOffset LastRun;
		public static DateTimeOffset NextRun;
		public static CancellationTokenSource? TimerCancellationTokenSource;

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

			NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
		}

		private async void NetworkChange_NetworkAddressChanged(object? sender, EventArgs e)
		{
			await NetworkChangeUpdateAsync(_logService);
		}

		protected override async Task ExecuteAsync(CancellationToken serviceStoppingToken)
		{
			// log DnsTube version
			var version = $"Running DnsTube {Application.RELEASE_TAG}";
			_logger.LogInformation(version);
			await _logService.WriteAsync(version, LogLevel.Information);

			_logger.LogInformation($"UI hosted at {_configuration["Url"]}");

			// log new release info
			var latestReleaseTag = await _githubService.GetLatestReleaseTagNameAsync();
			if (!string.IsNullOrWhiteSpace(latestReleaseTag) && !Application.RELEASE_TAG.Contains("beta") && latestReleaseTag != Application.RELEASE_TAG)
			{
				var msg = $"There is a newer release available ({latestReleaseTag}). See https://github.com/drittich/DnsTube/releases/latest for more information.";
				_logger.LogInformation(msg);
				await _logService.WriteAsync(msg, LogLevel.Information);
			}

			string? previousIpv4Address = null;
			string? previousIpv6Address = null;

			while (!serviceStoppingToken.IsCancellationRequested)
			{
				var msg = $"Worker running at: {DateTimeOffset.Now}";
				_logger.LogTrace(msg);

				var settings = await _settingsService.GetAsync(true);

				string? currentPublicIpv4Address = await GetIpAddressAsync(IpSupport.IPv4, previousIpv4Address, settings);
				string? currentPublicIpv6Address = await GetIpAddressAsync(IpSupport.IPv6, previousIpv6Address, settings);

				bool ipAddressChanged = false;
				if (previousIpv4Address != currentPublicIpv4Address || previousIpv6Address != currentPublicIpv6Address)
				{
					ipAddressChanged = true;
				}

				previousIpv4Address = currentPublicIpv4Address;
				previousIpv6Address = currentPublicIpv6Address;

				if (currentPublicIpv4Address != null)
					await _serverSentEventsService.SendEventAsync(new ServerSentEvent
					{
						Type = "ipv4-address",
						Data = new List<string> { currentPublicIpv4Address }
					});

				if (currentPublicIpv6Address != null)
					await _serverSentEventsService.SendEventAsync(new ServerSentEvent
					{
						Type = "ipv6-address",
						Data = new List<string> { currentPublicIpv6Address }
					});

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
					if (ipAddressChanged)
					{
						var selectedDomainsValid = await _cloudflareService.ValidateSelectedDomainsAsync();
						if (selectedDomainsValid)
							await DoUpdateAsync(currentPublicIpv4Address, currentPublicIpv6Address);

						await _serverSentEventsService.SendEventAsync(new ServerSentEvent
						{
							Type = "ip-address-changed",
							Data = new List<string> { "N/A" }
						});
					}
					else
					{
						if (isManualUpdate)
						{
							await _logService.WriteAsync("Public IP address has not changed", LogLevel.Information);
							isManualUpdate = false;
						}
					}
				}

				LastRun = DateTime.Now;
				var intervalMs = settings.UpdateIntervalMinutes * 60 * 1000;
				NextRun = DateTime.Now.AddMilliseconds(intervalMs);

				await _serverSentEventsService.SendEventAsync(new ServerSentEvent
				{
					Type = "last-run",
					Data = new List<string> { $"{LastRun:yyyy-MM-ddTHH:mm:ss}" }
				});
				await _serverSentEventsService.SendEventAsync(new ServerSentEvent
				{
					Type = "next-run",
					Data = new List<string> { $"{NextRun:yyyy-MM-ddTHH:mm:ss}" }
				});

				TimerCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serviceStoppingToken);
				try
				{
					await Task.Delay(intervalMs, TimerCancellationTokenSource.Token);
				}
				catch (TaskCanceledException) when (TimerCancellationTokenSource.IsCancellationRequested)
				{
				}
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
			var settings = await _settingsService.GetAsync();

			// if IPv6-only support was not specified, do the IPv4 update
			if (settings.ProtocolSupport != IpSupport.IPv6)
				await _cloudflareService.UpdateDnsRecordsAsync(IpSupport.IPv4, publicIpv4Address);

			// if IPv4-only support was not specified, do the IPv6 update
			if (settings.ProtocolSupport != IpSupport.IPv4)
				await _cloudflareService.UpdateDnsRecordsAsync(IpSupport.IPv6, publicIpv6Address);
		}

		public static async Task RequestManualUpdateAsync(ILogService logService)
		{
			isManualUpdate = true;
			await logService.WriteAsync("Manual update requested", LogLevel.Information);
			WorkerService.TimerCancellationTokenSource!.Cancel();
		}

		private static DateTime lastNetworkChangeUpdate = DateTime.MinValue;
		public static async Task NetworkChangeUpdateAsync(ILogService logService)
		{
			// This gets invoked for both ipv4 and ipv6 changes, so we need to debounce it
			if (DateTime.Now - lastNetworkChangeUpdate < TimeSpan.FromSeconds(1))
			{
				return;
			}

			lastNetworkChangeUpdate = DateTime.Now;
			isManualUpdate = true;

			// wait 5 seconds before updating to let the network settle
			int networkChangeDelaySeconds = 5;
			await logService.WriteAsync($"Network change detected, updating in {networkChangeDelaySeconds} seconds", LogLevel.Information);
			await Task.Delay(networkChangeDelaySeconds * 1000);

			TimerCancellationTokenSource!.Cancel();
		}
	}
}
