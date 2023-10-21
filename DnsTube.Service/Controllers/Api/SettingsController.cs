﻿using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;

using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DnsTube.Service.Controllers.Api
{
	[Route("api/[controller]")]
	[ApiController]
	public class SettingsController : ControllerBase
	{
		ILogger<SettingsController> _logger;
		ILogService _logService;
		ISettingsService _settingsService;
		IDbService _dbService;
		IIpAddressService _ipAddressService;

		public SettingsController(ILogger<SettingsController> logger, ILogService logService, ISettingsService settingsService, IDbService dbService, IIpAddressService ipAddressService)
		{
			_logger = logger;
			_logService = logService;
			_settingsService = settingsService;
			_dbService = dbService;
			_ipAddressService = ipAddressService;
		}

		// GET: api/<SettingsController>
		[HttpGet]
		public async Task<ISettings> GetAsync()
		{
			var settings = await _settingsService.GetAsync();
			return settings;
		}

		// POST api/<SettingsController>
		[HttpPost]
		public async Task<ISettings> Post([FromForm] Settings settings)
		{
			// persist settings not saved through the Setting UI
			var currentSettings = await _settingsService.GetAsync();
			settings.SelectedDomains = currentSettings.SelectedDomains;
			settings.PublicIpv4Address = currentSettings.PublicIpv4Address;
			settings.PublicIpv6Address = currentSettings.PublicIpv6Address;

			await _settingsService.SaveAsync(settings);
			return await _settingsService.GetAsync();
		}

		// POST api/<SettingsController>/entries
		[HttpPost("entries")]
		public async Task<ISettings> Post([FromBody] IList<SelectedDomain> domains)
		{
			await _settingsService.SaveDomainsAsync(domains);
			return await _settingsService.GetAsync();
		}

		// GET api/<SettingsController>/dbpath
		[HttpGet("dbpath")]
		public string Get()
		{
			return _dbService.GetDbPath();
		}

		public record RunInfo(string LastRun, string NextRun);

		// GET api/<SettingsController>/runinfo
		[HttpGet]
		[Route("runinfo")]
		public async Task<RunInfo> GetRunInfo()
		{

			while (WorkerService.LastRun == DateTimeOffset.MinValue)
				await Task.Delay(100);

			return new RunInfo(WorkerService.LastRun.ToString("yyyy-MM-ddTHH:mm:sszzz"), WorkerService.NextRun.ToString("yyyy-MM-ddTHH:mm:sszzz"));
		}


		// GET api/<SettingsController>/adapters
		[HttpGet]
		[Route("adapters")]
		public List<NetworkAdapter> GetAdapters()
		{
			return _ipAddressService.GetNetworkAdapters()
				.OrderBy(a => a.Name)
				.ToList();
		}
	}
}
