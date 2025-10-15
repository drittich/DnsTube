using System.Text.Json;
using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;

namespace DnsTube.Cli.Services
{
	/// <summary>
	/// Manages alternate configuration files
	/// </summary>
	public class ConfigFileService : IConfigFileService
	{
		public bool IsStandaloneMode { get; set; }
		public string? ConfigFilePath { get; set; }

		public async Task<ISettings> LoadFromFileAsync(string path)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException($"Configuration file not found: {path}");
			}

			var json = await File.ReadAllTextAsync(path);
			var dto = JsonSerializer.Deserialize<SettingsDTO>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (dto == null)
			{
				throw new InvalidOperationException($"Failed to deserialize configuration from: {path}");
			}

			var settings = new Settings
			{
				ApiKeyOrToken = dto.ApiKeyOrToken ?? string.Empty,
				EmailAddress = dto.EmailAddress ?? string.Empty,
				IPv4_API = dto.IPv4_API ?? "https://api.ipify.org/",
				IPv6_API = dto.IPv6_API ?? "https://api64.ipify.org/",
				IsUsingToken = dto.IsUsingToken,
				ProtocolSupport = (IpSupport)dto.ProtocolSupport,
				PublicIpv4Address = dto.PublicIpv4Address ?? string.Empty,
				PublicIpv6Address = dto.PublicIpv6Address ?? string.Empty,
				SelectedDomains = dto.SelectedDomains ?? new List<SelectedDomain>(),
				SkipCheckForNewReleases = dto.SkipCheckForNewReleases,
				UpdateIntervalMinutes = dto.UpdateIntervalMinutes,
				ZoneIDs = dto.ZoneIDs
			};

			return settings;
		}

		public async Task SaveToFileAsync(string path, ISettings settings)
		{
			var dto = new SettingsDTO
			{
				ApiKeyOrToken = settings.ApiKeyOrToken,
				EmailAddress = settings.EmailAddress,
				IPv4_API = settings.IPv4_API,
				IPv6_API = settings.IPv6_API,
				IsUsingToken = settings.IsUsingToken,
				ProtocolSupport = (int)settings.ProtocolSupport,
				PublicIpv4Address = settings.PublicIpv4Address,
				PublicIpv6Address = settings.PublicIpv6Address,
				SelectedDomains = settings.SelectedDomains,
				SkipCheckForNewReleases = settings.SkipCheckForNewReleases,
				UpdateIntervalMinutes = settings.UpdateIntervalMinutes,
				ZoneIDs = settings.ZoneIDs
			};

			var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});

			// Ensure directory exists
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory))
			{
				Directory.CreateDirectory(directory);
			}

			await File.WriteAllTextAsync(path, json);
		}

		public async Task<bool> ValidateFileAsync(string path)
		{
			try
			{
				if (!File.Exists(path))
				{
					return false;
				}

				var json = await File.ReadAllTextAsync(path);
				var dto = JsonSerializer.Deserialize<SettingsDTO>(json, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				return dto != null;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// DTO for JSON serialization/deserialization
		/// </summary>
		private class SettingsDTO
		{
			public string? ApiKeyOrToken { get; set; }
			public string? EmailAddress { get; set; }
			public string? IPv4_API { get; set; }
			public string? IPv6_API { get; set; }
			public bool IsUsingToken { get; set; }
			public int ProtocolSupport { get; set; }
			public string? PublicIpv4Address { get; set; }
			public string? PublicIpv6Address { get; set; }
			public List<SelectedDomain>? SelectedDomains { get; set; }
			public bool SkipCheckForNewReleases { get; set; }
			public int UpdateIntervalMinutes { get; set; }
			public string? ZoneIDs { get; set; }
		}
	}
}