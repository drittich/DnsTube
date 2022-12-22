using DnsTube.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DnsTube.Service.Controllers.Api
{
	// Note: I renamed this from LogController to HistoryController, because uBlock
	// was blocking the API requests
	[Route("api/[controller]")]
	[ApiController]
	public class HistoryController : ControllerBase
	{
		ILogger<HistoryController> _logger;
		ILogService _logService;

		public HistoryController(ILogger<HistoryController> logger, ILogService logService)
		{
			_logger = logger;
			_logService = logService;
		}

		// GET: api/<HistoryController>
		[HttpGet]
		public async Task<IEnumerable<LogEntry>> GetAsync([FromQuery] int? pageSize, int? lastId = null)
		{
			return await _logService.GetAsync(pageSize, lastId);
		}

		// DELETE api/<HistoryController>
		[HttpDelete]
		public async Task Delete()
		{
			await _logService.ClearAsync();
		}
	}
}
