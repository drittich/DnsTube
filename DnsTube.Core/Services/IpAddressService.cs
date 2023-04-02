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
			var attempts = 0;

			var settings = await _settingsService.GetAsync();
			var url = protocol == IpSupport.IPv4 ? settings.IPv4_API : settings.IPv6_API;

			var httpClient = _httpClientFactory.CreateClient(HttpClientName.IpAddress.ToString());

			// TODO: replace with Polly
			while (publicIpAddress == null && attempts < maxAttempts)
			{
				try
				{
					attempts++;
					var response = await httpClient.GetStringAsync(url);
					var candidatePublicIpAddress = response.Replace("\n", "");

					if (protocol == IpSupport.IPv6 && !IsValidIpAddress(protocol, candidatePublicIpAddress))
					{
						return "";
					}
					if (protocol == IpSupport.IPv4 && !IsValidIpAddress(protocol, candidatePublicIpAddress))
					{
						throw new Exception($"Malformed response, expected IP address: {response}");
					}

					publicIpAddress = candidatePublicIpAddress;

					//save to settings
					var ipAddressChanged = false;
					if (protocol == IpSupport.IPv4)
					{
						if (settings.PublicIpv4Address != publicIpAddress)
							ipAddressChanged = true;

						settings.PublicIpv4Address = publicIpAddress;
					}
					else
					{
						if (settings.PublicIpv6Address != publicIpAddress)
							ipAddressChanged = true;

						settings.PublicIpv6Address = publicIpAddress;
					}

					if (ipAddressChanged)
						await _settingsService.SaveAsync(settings);
				}
				catch (Exception e)
				{
					if (attempts >= maxAttempts)
						await _logService.WriteAsync(e.Message, LogLevel.Error);
				}
			}
			return publicIpAddress;
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
	}
}
