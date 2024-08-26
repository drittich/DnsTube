using DnsTube.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace DnsTube.Service.Controllers.Api
{
	[Route("api/[controller]")]
	[ApiController]
	public class DnsController : ControllerBase
	{
		private readonly ILogger<DnsController> _logger;
		private readonly ILogService _logService;
		private readonly ISettingsService _settingsService;
		private readonly ICloudflareService _cloudflareService;

		public DnsController(ILogger<DnsController> logger, ILogService logService, ISettingsService settingsService, ICloudflareService cloudflareService)
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
		public async Task<string> GetUpdateAsync(int id)
		{
			await WorkerService.RequestManualUpdateAsync(_logService);
			return "ok";
		}
	}
}
