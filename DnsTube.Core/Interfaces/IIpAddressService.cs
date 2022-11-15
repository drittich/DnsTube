using DnsTube.Core.Enums;

namespace DnsTube.Core.Interfaces
{
	public interface IIpAddressService
	{
		Task<string?> GetPublicIpAddressAsync(IpSupport protocol);
		bool IsValidIpAddress(IpSupport protocol, string ipString);
	}
}
