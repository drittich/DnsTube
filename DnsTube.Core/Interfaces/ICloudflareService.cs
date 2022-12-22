using DnsTube.Core.Enums;

namespace DnsTube.Core.Interfaces
{
	public interface ICloudflareService
	{
		/// <summary>
		/// Returns true if an update was performed
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		Task<bool> UpdateDnsRecordsAsync(IpSupport protocol, string publicIpAddress);
		Task<bool> ValidateSelectedDomainsAsync();
		Task<List<Models.Dns.Result>> GetAllDnsRecordsByZoneAsync();
	}
}
