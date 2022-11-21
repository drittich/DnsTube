using DnsTube.Core.Interfaces;

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

		public SettingsController(ILogger<SettingsController> logger, ILogService logService, ISettingsService settingsService, IDbService dbService)
		{
			_logger = logger;
			_logService = logService;
			_settingsService = settingsService;
			_dbService = dbService;
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
		public RunInfo GetRunInfo()
		{
			return new RunInfo(WorkerService.LastRun.ToString("yyyy-MM-ddTHH:mm:ss"), WorkerService.NextRun.ToString("yyyy-MM-ddTHH:mm:ss"));
		}
	}
}
