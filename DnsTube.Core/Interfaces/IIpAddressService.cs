using DnsTube.Core.Enums;
using DnsTube.Core.Models;

namespace DnsTube.Core.Interfaces
{
	public interface IIpAddressService
	{
		Task<string?> GetPublicIpAddressAsync(IpSupport protocol);
		bool IsValidIpAddress(IpSupport protocol, string ipString);
		List<NetworkAdapter> GetNetworkAdapters();
	}
}
