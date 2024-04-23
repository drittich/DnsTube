using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;

using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Services
{
	public class IpAddressService : IIpAddressService
	{
		private readonly ISettingsService _settingsService;
		private readonly ILogService _logService;
		private readonly IHttpClientFactory _httpClientFactory;

		public IpAddressService(ISettingsService settingsService, ILogService logService, IHttpClientFactory httpClientFactory)
		{
			_settingsService = settingsService;
			_logService = logService;
			_httpClientFactory = httpClientFactory;
		}

		public async Task<string?> GetPublicIpAddressAsync(IpSupport protocol)
		{
			string? publicIpAddress = null;
			var maxAttempts = 3;

			var settings = await _settingsService.GetAsync();
			var url = protocol == IpSupport.IPv4 ? settings.IPv4_API : settings.IPv6_API;
			var httpClientName = protocol == IpSupport.IPv4 ? HttpClientName.IpAddressV4 : HttpClientName.IpAddressV6;
			var httpClient = _httpClientFactory.CreateClient(httpClientName.ToString());

			for (var attempts = 0; attempts < maxAttempts; attempts++)
			{
				try
				{
					var response = await httpClient.GetStringAsync(url);
					var candidatePublicIpAddress = response.Replace("\n", "");

					if (!IsValidIpAddress(protocol, candidatePublicIpAddress))
					{
						if (protocol == IpSupport.IPv6)
						{
							return string.Empty;
						}

						throw new Exception($"Malformed response, expected IP address: {response}");
					}

					publicIpAddress = candidatePublicIpAddress;

					await SaveIpAddressIfChanged(protocol, publicIpAddress, settings);
					break;
				}
				catch (Exception e)
				{
					if (attempts >= maxAttempts - 1)
					{
						await _logService.WriteAsync(e.Message, LogLevel.Error);
					}
				}
			}

			return publicIpAddress;
		}

		private async Task SaveIpAddressIfChanged(IpSupport protocol, string ipAddress, ISettings settings)
		{
			if (protocol == IpSupport.IPv4 && settings.PublicIpv4Address != ipAddress)
			{
				settings.PublicIpv4Address = ipAddress;
				await _settingsService.SaveAsync(settings);
			}
			else if (protocol == IpSupport.IPv6 && settings.PublicIpv6Address != ipAddress)
			{
				settings.PublicIpv6Address = ipAddress;
				await _settingsService.SaveAsync(settings);
			}
		}

		public bool IsValidIpAddress(IpSupport protocol, string ipString)
		{
			if (string.IsNullOrWhiteSpace(ipString))
				return false;

			if (protocol == IpSupport.IPv4)
			{
				// probably a more efficient way to parse than using regex
				string[] splitValues = ipString.Split('.');
				if (splitValues.Length != 4)
					return false;

				return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
			}
			else
			{
				var regex = new Regex(Application.Ipv6RegexExact);
				var match = regex.Match(ipString);
				return match.Success;
			}
		}

		public List<NetworkAdapter> GetNetworkAdapters()
		{
			var adapters = new List<NetworkAdapter>();
			var candidateAdapters = NetworkInterface.GetAllNetworkInterfaces();

			foreach (var adapter in candidateAdapters)
			{
				var interNetworkAddresses = adapter
					.GetIPProperties().UnicastAddresses
					.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

				if (interNetworkAddresses.Any())
				{
					adapters.Add(new NetworkAdapter()
					{
						Name = adapter.Name,
						IpAddress = interNetworkAddresses.First().Address.ToString()
					});
				}
			}

			return adapters;
		}
	}
}
