using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DnsTube.Service.Controllers.Api
{
	[Route("api/[controller]")]
	[ApiController]
	public class IpController : ControllerBase
	{
		private readonly ILogger<IpController> _logger;
		private readonly ILogService _logService;
		private readonly IIpAddressService _ipAddressService;

		public record Ip(string? Ipv4, string? Ipv6);

		public IpController(ILogger<IpController> logger, ILogService logService, IIpAddressService ipAddressService)
		{
			_logger = logger;
			_logService = logService;
			_ipAddressService = ipAddressService;
		}
		
		// GET: api/<IpController>
		[HttpGet]
		public async Task<Ip> Get()
		{
			var ipv4 = await _ipAddressService.GetPublicIpAddressAsync(IpSupport.IPv4);
			var ipv6 = await _ipAddressService.GetPublicIpAddressAsync(IpSupport.IPv6);
			return new Ip(Ipv4: ipv4, Ipv6: ipv6);
		}

		// POST api/<IpController>
		[HttpPost]
		public void Post([FromBody] string value)
		{
		}

		// PUT api/<IpController>/5
		[HttpPut("{id}")]
		public void Put(int id, [FromBody] string value)
		{
		}

		// DELETE api/<IpController>/5
		[HttpDelete("{id}")]
		public void Delete(int id)
		{
		}
	}
}
