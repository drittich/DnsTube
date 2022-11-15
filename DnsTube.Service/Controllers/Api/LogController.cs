using Microsoft.AspNetCore.Mvc;
using DnsTube.Core.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DnsTube.Service.Controllers.Api
{
	[Route("api/[controller]")]
	[ApiController]
	public class LogController : ControllerBase
	{
		ILogger<LogController> _logger;
		ILogService _logService;

		public LogController(ILogger<LogController> logger, ILogService logService)
		{
			_logger = logger;
			_logService = logService;
		}

		// GET: api/<LogController>
		[HttpGet]
		public async Task<IEnumerable<LogEntry>> GetAsync()
		{
			return await _logService.GetAsync();
		}

		// DELETE api/<LogController>
		[HttpDelete]
		public async Task Delete()
		{
			await _logService.ClearAsync();
		}
	}
}
