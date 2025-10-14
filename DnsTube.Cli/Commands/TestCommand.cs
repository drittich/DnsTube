using System.CommandLine;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using DnsTube.Cli.Models;
using DnsTube.Cli.Services;
using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace DnsTube.Cli.Commands
{
	/// <summary>
	/// Test command - Test configuration and connectivity
	/// </summary>
	public static class TestCommand
	{
		public static Command Create(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("test", "Test configuration and connectivity");

			// Add subcommands
			command.AddCommand(CreateConnectionCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateApiCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateIpCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));

			return command;
		}

		#region test connection

		private static Command CreateConnectionCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("connection", "Test network connectivity");

			var ipv4Option = new Option<bool>(
				aliases: new[] { "--ipv4" },
				description: "Test IPv4 connectivity only");

			var ipv6Option = new Option<bool>(
				aliases: new[] { "--ipv6" },
				description: "Test IPv6 connectivity only");

			command.AddOption(ipv4Option);
			command.AddOption(ipv6Option);

			command.SetHandler(async (bool ipv4Only, bool ipv6Only, string? configFile, string? configFileOnly,
				bool json, bool verbose, bool noColor) =>
			{
				var options = new CliOptions
				{
					ConfigFile = configFile,
					ConfigFileOnly = configFileOnly,
					JsonOutput = json,
					Verbose = verbose,
					NoColor = noColor
				};

				await ExecuteConnectionTestAsync(options, ipv4Only, ipv6Only);
			},
			ipv4Option, ipv6Option, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteConnectionTestAsync(CliOptions options, bool ipv4Only, bool ipv6Only)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				if (ipv4Only && ipv6Only)
				{
					output.WriteError("Cannot specify both --ipv4 and --ipv6");
					return 1;
				}

				var settings = await settingsService.GetAsync();
				var results = new List<ConnectionTestResult>();

				// Determine which protocols to test
				bool testIpv4 = !ipv6Only && (ipv4Only || settings.ProtocolSupport != IpSupport.IPv6);
				bool testIpv6 = !ipv4Only && (ipv6Only || settings.ProtocolSupport != IpSupport.IPv4);

				if (!options.JsonOutput)
				{
					output.WriteInfo("Testing network connectivity...");
					output.WriteLine();
				}

				// Test IPv4 connectivity
				if (testIpv4)
				{
					var ipv4Result = await TestConnectivityAsync(IpSupport.IPv4, settings.IPv4_API, output, options);
					results.Add(ipv4Result);
				}

				// Test IPv6 connectivity
				if (testIpv6)
				{
					var ipv6Result = await TestConnectivityAsync(IpSupport.IPv6, settings.IPv6_API, output, options);
					results.Add(ipv6Result);
				}

				// Output results
				if (options.JsonOutput)
				{
					output.WriteJson(new
					{
						success = results.All(r => r.Success),
						timestamp = DateTime.UtcNow,
						results = results.Select(r => new
						{
							protocol = r.Protocol.ToString(),
							success = r.Success,
							latencyMs = r.LatencyMs,
							endpoint = r.Endpoint,
							error = r.Error
						})
					});
				}
				else
				{
					output.WriteLine();
					foreach (var result in results)
					{
						if (result.Success)
						{
							output.WriteSuccess($"{result.Protocol} connectivity test passed ({result.LatencyMs}ms)");
						}
						else
						{
							output.WriteError($"{result.Protocol} connectivity test failed: {result.Error}");
						}
					}

					output.WriteLine();
					if (results.All(r => r.Success))
					{
						output.WriteSuccess("All connectivity tests passed");
					}
					else
					{
						output.WriteError("Some connectivity tests failed");
						output.WriteInfo("Suggestions:");
						output.WriteInfo("  • Check your internet connection");
						output.WriteInfo("  • Verify firewall settings");
						output.WriteInfo("  • Ensure DNS resolution is working");
					}
				}

				return results.All(r => r.Success) ? 0 : 3;
			}
			catch (Exception ex)
			{
				return HandleError(options, ex, 3);
			}
		}

		private static async Task<ConnectionTestResult> TestConnectivityAsync(IpSupport protocol, 
			string apiUrl, IOutputService output, CliOptions options)
		{
			var result = new ConnectionTestResult { Protocol = protocol, Endpoint = apiUrl };
			var sw = Stopwatch.StartNew();

			try
			{
				if (!options.JsonOutput && options.Verbose)
				{
					output.WriteInfo($"Testing {protocol} connectivity to {apiUrl}...");
				}

				using var httpClient = new HttpClient();
				httpClient.Timeout = TimeSpan.FromSeconds(10);

				var response = await httpClient.GetAsync(apiUrl);
				sw.Stop();

				result.LatencyMs = (int)sw.ElapsedMilliseconds;

				if (response.IsSuccessStatusCode)
				{
					result.Success = true;
					if (!options.JsonOutput && options.Verbose)
					{
						output.WriteSuccess($"{protocol} endpoint responded ({result.LatencyMs}ms)");
					}
				}
				else
				{
					result.Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
				}
			}
			catch (Exception ex)
			{
				sw.Stop();
				result.LatencyMs = (int)sw.ElapsedMilliseconds;
				result.Error = ex.Message;
			}

			return result;
		}

		#endregion

		#region test api

		private static Command CreateApiCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("api", "Test Cloudflare API authentication");

			command.SetHandler(async (string? configFile, string? configFileOnly,
				bool json, bool verbose, bool noColor) =>
			{
				var options = new CliOptions
				{
					ConfigFile = configFile,
					ConfigFileOnly = configFileOnly,
					JsonOutput = json,
					Verbose = verbose,
					NoColor = noColor
				};

				await ExecuteApiTestAsync(options);
			},
			configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteApiTestAsync(CliOptions options)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();

				if (!options.JsonOutput)
				{
					output.WriteInfo("Testing Cloudflare API authentication...");
					output.WriteLine();
				}

				var settings = await settingsService.GetAsync();
				var validationError = settingsService.ValidateSettings(settings);

				var results = new ApiTestResult
				{
					AuthMethod = settings.IsUsingToken ? "Token" : "API Key",
					Email = settings.EmailAddress
				};

				// Validate configuration
				if (validationError != null)
				{
					results.ConfigValid = false;
					results.Error = validationError;

					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, results });
					}
					else
					{
						output.WriteError($"Configuration invalid: {validationError}");
					}
					return 4;
				}

				results.ConfigValid = true;

				// Test API authentication
				try
				{
					if (!options.JsonOutput && options.Verbose)
					{
						output.WriteInfo("Attempting to authenticate with Cloudflare API...");
					}

					var sw = Stopwatch.StartNew();
					var zones = await GetZonesForTestAsync(cloudflareService);
					sw.Stop();

					results.AuthSuccess = true;
					results.LatencyMs = (int)sw.ElapsedMilliseconds;
					results.AccessibleZones = zones.Select(z => z.name).ToList();

					if (!options.JsonOutput)
					{
						output.WriteSuccess("API authentication successful");
						output.WriteInfo($"Response time: {results.LatencyMs}ms");
						output.WriteInfo($"Accessible zones: {results.AccessibleZones.Count}");

						if (options.Verbose && results.AccessibleZones.Any())
						{
							output.WriteLine();
							output.WriteInfo("Zone list:");
							foreach (var zone in results.AccessibleZones)
							{
								output.WriteInfo($"  • {zone}");
							}
						}
					}
				}
				catch (Exception ex)
				{
					results.AuthSuccess = false;
					results.Error = ex.Message;

					if (!options.JsonOutput)
					{
						output.WriteError($"API authentication failed: {ex.Message}");
						output.WriteLine();
						output.WriteInfo("Suggestions:");
						output.WriteInfo($"  • Verify your {results.AuthMethod} is correct");
						if (!settings.IsUsingToken)
						{
							output.WriteInfo("  • Verify your email address is correct");
						}
						output.WriteInfo("  • Check that your credentials have the required permissions");
						output.WriteInfo("  • Consider using an API Token (recommended over API Key)");
					}
				}

				// Output results
				if (options.JsonOutput)
				{
					output.WriteJson(new
					{
						success = results.AuthSuccess,
						timestamp = DateTime.UtcNow,
						results = new
						{
							configValid = results.ConfigValid,
							authMethod = results.AuthMethod,
							email = results.Email,
							authSuccess = results.AuthSuccess,
							latencyMs = results.LatencyMs,
							accessibleZoneCount = results.AccessibleZones.Count,
							accessibleZones = results.AccessibleZones,
							error = results.Error
						}
					});
				}
				else
				{
					output.WriteLine();
					if (results.AuthSuccess)
					{
						output.WriteSuccess("API test completed successfully");
					}
					else
					{
						output.WriteError("API test failed");
					}
				}

				return results.AuthSuccess ? 0 : 4;
			}
			catch (Exception ex)
			{
				return HandleError(options, ex, 4);
			}
		}

		private static async Task<List<DnsTube.Core.Models.Zone.Zone>> GetZonesForTestAsync(ICloudflareService cloudflareService)
		{
			// Use reflection to call ListZoneesAsync which is not in the interface
			var method = cloudflareService.GetType().GetMethod("ListZoneesAsync");
			if (method != null)
			{
				var task = (Task<List<DnsTube.Core.Models.Zone.Zone>>?)method.Invoke(cloudflareService, null);
				if (task != null)
				{
					return await task;
				}
			}
			throw new Exception("Unable to access zone list method");
		}

		#endregion

		#region test ip

		private static Command CreateIpCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("ip", "Test IP address retrieval");

			var adapterOption = new Option<string?>(
				aliases: new[] { "--adapter" },
				description: "Test specific network adapter");

			command.AddOption(adapterOption);

			command.SetHandler(async (string? adapter, string? configFile, string? configFileOnly,
				bool json, bool verbose, bool noColor) =>
			{
				var options = new CliOptions
				{
					ConfigFile = configFile,
					ConfigFileOnly = configFileOnly,
					JsonOutput = json,
					Verbose = verbose,
					NoColor = noColor
				};

				await ExecuteIpTestAsync(options, adapter);
			},
			adapterOption, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteIpTestAsync(CliOptions options, string? adapterName)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var ipAddressService = serviceProvider.GetRequiredService<IIpAddressService>();

				var settings = await settingsService.GetAsync();
				var results = new List<IpTestResult>();

				if (!options.JsonOutput)
				{
					output.WriteInfo("Testing IP address retrieval...");
					output.WriteLine();
				}

				// Test specific adapter if requested
				if (!string.IsNullOrEmpty(adapterName))
				{
					if (!options.JsonOutput && options.Verbose)
					{
						output.WriteInfo($"Testing adapter: {adapterName}");
					}

					var ipv4Result = await TestIpRetrievalAsync(IpSupport.IPv4, ipAddressService, 
						settings, output, options, adapterName);
					if (ipv4Result != null) results.Add(ipv4Result);

					var ipv6Result = await TestIpRetrievalAsync(IpSupport.IPv6, ipAddressService, 
						settings, output, options, adapterName);
					if (ipv6Result != null) results.Add(ipv6Result);
				}
				else
				{
					// Test public IP retrieval
					if (settings.ProtocolSupport != IpSupport.IPv6)
					{
						var ipv4Result = await TestIpRetrievalAsync(IpSupport.IPv4, ipAddressService, 
							settings, output, options, null);
						if (ipv4Result != null) results.Add(ipv4Result);
					}

					if (settings.ProtocolSupport != IpSupport.IPv4)
					{
						var ipv6Result = await TestIpRetrievalAsync(IpSupport.IPv6, ipAddressService, 
							settings, output, options, null);
						if (ipv6Result != null) results.Add(ipv6Result);
					}

					// List available adapters
					if (!options.JsonOutput && options.Verbose)
					{
						output.WriteLine();
						output.WriteInfo("Available network adapters:");
						var adapters = ipAddressService.GetNetworkAdapters();
						foreach (var adapter in adapters)
						{
							output.WriteInfo($"  • {adapter.Name}: {adapter.IpAddress}");
						}
					}
				}

				// Output results
				if (options.JsonOutput)
				{
					var adapters = ipAddressService.GetNetworkAdapters();
					output.WriteJson(new
					{
						success = results.All(r => r.Success),
						timestamp = DateTime.UtcNow,
						results = results.Select(r => new
						{
							protocol = r.Protocol.ToString(),
							source = r.Source,
							success = r.Success,
							ipAddress = r.IpAddress,
							valid = r.Valid,
							latencyMs = r.LatencyMs,
							error = r.Error
						}),
						availableAdapters = adapters.Select(a => new
						{
							name = a.Name,
							ipAddress = a.IpAddress
						})
					});
				}
				else
				{
					output.WriteLine();
					foreach (var result in results)
					{
						if (result.Success && result.Valid)
						{
							output.WriteSuccess($"{result.Protocol} from {result.Source}: {result.IpAddress} ({result.LatencyMs}ms)");
						}
						else if (result.Success && !result.Valid)
						{
							output.WriteWarning($"{result.Protocol} from {result.Source}: Invalid format - {result.IpAddress}");
						}
						else
						{
							output.WriteError($"{result.Protocol} from {result.Source}: {result.Error}");
						}
					}

					output.WriteLine();
					if (results.All(r => r.Success && r.Valid))
					{
						output.WriteSuccess("All IP retrieval tests passed");
					}
					else if (results.Any(r => !r.Success))
					{
						output.WriteError("Some IP retrieval tests failed");
						output.WriteInfo("Suggestions:");
						output.WriteInfo("  • Check your internet connection");
						output.WriteInfo("  • Verify the API URLs in configuration");
						output.WriteInfo("  • For adapter tests, ensure the adapter name is correct");
					}
					else if (results.Any(r => !r.Valid))
					{
						output.WriteWarning("IP addresses retrieved but format validation failed");
					}
				}

				return results.All(r => r.Success && r.Valid) ? 0 : 3;
			}
			catch (Exception ex)
			{
				return HandleError(options, ex, 3);
			}
		}

		private static async Task<IpTestResult?> TestIpRetrievalAsync(IpSupport protocol, 
			IIpAddressService ipAddressService, ISettings settings, IOutputService output, 
			CliOptions options, string? adapterName)
		{
			var result = new IpTestResult { Protocol = protocol };
			var sw = Stopwatch.StartNew();

			try
			{
				if (!string.IsNullOrEmpty(adapterName))
				{
					// Test adapter IP retrieval
					result.Source = $"adapter '{adapterName}'";

					if (!options.JsonOutput && options.Verbose)
					{
						output.WriteInfo($"Retrieving {protocol} from {result.Source}...");
					}

					var ip = ipAddressService.GetIpAddressFromAdapter(adapterName, protocol);
					sw.Stop();
					result.LatencyMs = (int)sw.ElapsedMilliseconds;

					if (!string.IsNullOrEmpty(ip))
					{
						result.Success = true;
						result.IpAddress = ip;
						result.Valid = ipAddressService.IsValidIpAddress(protocol, ip);
					}
					else
					{
						result.Error = $"No {protocol} address found on adapter";
					}
				}
				else
				{
					// Test public IP retrieval
					var apiUrl = protocol == IpSupport.IPv4 ? settings.IPv4_API : settings.IPv6_API;
					result.Source = $"API ({apiUrl})";

					if (!options.JsonOutput && options.Verbose)
					{
						output.WriteInfo($"Retrieving {protocol} from {result.Source}...");
					}

					var ip = await ipAddressService.GetPublicIpAddressAsync(protocol);
					sw.Stop();
					result.LatencyMs = (int)sw.ElapsedMilliseconds;

					if (!string.IsNullOrEmpty(ip))
					{
						result.Success = true;
						result.IpAddress = ip;
						result.Valid = ipAddressService.IsValidIpAddress(protocol, ip);

						if (!options.JsonOutput && options.Verbose && !result.Valid)
						{
							output.WriteWarning($"Retrieved IP '{ip}' failed validation");
						}
					}
					else
					{
						result.Error = "Unable to retrieve IP address";
					}
				}
			}
			catch (Exception ex)
			{
				sw.Stop();
				result.LatencyMs = (int)sw.ElapsedMilliseconds;
				result.Error = ex.Message;
			}

			return result;
		}

		#endregion

		#region Helper Classes

		private class ConnectionTestResult
		{
			public IpSupport Protocol { get; set; }
			public bool Success { get; set; }
			public int LatencyMs { get; set; }
			public string Endpoint { get; set; } = string.Empty;
			public string? Error { get; set; }
		}

		private class ApiTestResult
		{
			public bool ConfigValid { get; set; }
			public string AuthMethod { get; set; } = string.Empty;
			public string Email { get; set; } = string.Empty;
			public bool AuthSuccess { get; set; }
			public int LatencyMs { get; set; }
			public List<string> AccessibleZones { get; set; } = new();
			public string? Error { get; set; }
		}

		private class IpTestResult
		{
			public IpSupport Protocol { get; set; }
			public string Source { get; set; } = string.Empty;
			public bool Success { get; set; }
			public string? IpAddress { get; set; }
			public bool Valid { get; set; }
			public int LatencyMs { get; set; }
			public string? Error { get; set; }
		}

		private static int HandleError(CliOptions options, Exception ex, int exitCode)
		{
			var serviceProvider = Program.BuildServiceProvider(options);
			var output = serviceProvider.GetRequiredService<IOutputService>();

			if (options.JsonOutput)
			{
				output.WriteJson(new { success = false, error = ex.Message });
			}
			else
			{
				output.WriteError($"Error: {ex.Message}");
				if (options.Verbose)
				{
					output.WriteError(ex.StackTrace ?? "");
				}
			}
			return exitCode;
		}

		#endregion
	}
}