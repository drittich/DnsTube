using DnsTube.Core.Enums;

namespace DnsTube.Core.Interfaces
{
	public interface ICloudflareService
	{
		Task<bool> UpdateDnsRecordsAsync(IpSupport protocol, string publicIpAddress);
		Task<bool> ValidateSelectedDomainsAsync();
		Task<List<Models.Dns.Result>> GetAllDnsRecordsByZoneAsync();
	}
}
