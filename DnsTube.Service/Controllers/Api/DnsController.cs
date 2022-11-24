using System.Linq;
using DnsTube.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DnsTube.Service.Controllers.Api
{
	[Route("api/[controller]")]
	[ApiController]
	public class DnsController : ControllerBase
	{
		ILogger<SettingsController> _logger;
		ILogService _logService;
		ISettingsService _settingsService;
		ICloudflareService _cloudflareService;

		public DnsController(ILogger<SettingsController> logger, ILogService logService, ISettingsService settingsService, ICloudflareService cloudflareService)
		{
			_logger = logger;
			_logService = logService;
			_settingsService = settingsService;
			_cloudflareService = cloudflareService;
		}

		// GET: api/<DnsController>
		/// <summary>
		/// Gets the DNS entries from Cloudflare, determines which are selected for update
		/// and the returns the info to the client.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<IEnumerable<Core.Models.DnsEntry>> Get()
		{
			var zones = await _cloudflareService.GetAllDnsRecordsByZoneAsync();
			var settings = await _settingsService.GetAsync();

			//map zones to DnsEntryViewItem
			var dnsEntries = zones.Select(d => new Core.Models.DnsEntry
			{
				UpdateCloudflare = settings.SelectedDomains.Any(s => s.ZoneName == d.zone_name && s.DnsName == d.name && s.Type == d.type),
				DnsName = d.name,
				Type = d.type,
				Address = d.content,
				TTL = d.ttl,
				Proxied = d.proxied,
				ZoneName = d.zone_name
			});
			
			return dnsEntries;
		}

		// POST api/<DnsController>/update
		[HttpPost("update")]
		public string GetUpdate(int id)
		{
			WorkerService.TimerCancellationTokenSource.Cancel();
			return "ok";
		}
	}
}
