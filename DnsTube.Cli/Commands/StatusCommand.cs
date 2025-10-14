using System.CommandLine;
using DnsTube.Cli.Models;
using DnsTube.Cli.Services;
using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DnsTube.Cli.Commands
{
	/// <summary>
	/// Status command - Display current status
	/// </summary>
	public static class StatusCommand
	{
		public static Command Create(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("status", "Display current status and configuration");

			var checkIpOption = new Option<bool>(
				aliases: new[] { "--check-ip" },
				description: "Fetch and display current public IP");

			var checkServiceOption = new Option<bool>(
				aliases: new[] { "--check-service" },
				description: "Check if DnsTube service is running");

			command.AddOption(checkIpOption);
			command.AddOption(checkServiceOption);

			command.SetHandler(async (bool checkIp, bool checkService, string? configFile, string? configFileOnly,
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

				await ExecuteAsync(options, checkIp, checkService);
			},
			checkIpOption, checkServiceOption, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteAsync(CliOptions options, bool checkIp, bool checkService)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var ipAddressService = serviceProvider.GetRequiredService<IIpAddressService>();

				if (options.JsonOutput)
				{
					var statusData = await GetStatusDataAsync(settingsService, ipAddressService, checkIp, checkService, options);
					output.WriteJson(statusData);
					return 0;
				}

				// Display status in human-readable format
				output.WriteInfo("DnsTube Status");
				output.WriteLine();

				// Configuration source
				if (options.IsStandaloneMode)
				{
					output.WriteInfo($"Configuration: {options.ConfigFileOnly} (standalone mode)");
				}
				else if (!string.IsNullOrEmpty(options.ConfigFile))
				{
					output.WriteInfo($"Configuration: {options.ConfigFile}");
					var dbService = serviceProvider.GetRequiredService<IDbService>();
					output.WriteInfo($"Database: {dbService.GetDbPath()}");
				}
				else
				{
					var dbService = serviceProvider.GetRequiredService<IDbService>();
					output.WriteInfo($"Database: {dbService.GetDbPath()}");
				}

				output.WriteLine();

				// Load and validate settings
				var settings = await settingsService.GetAsync();
				var validationError = settingsService.ValidateSettings(settings);

				if (validationError != null)
				{
					output.WriteError($"Configuration: Invalid - {validationError}");
				}
				else
				{
					output.WriteSuccess("Configuration: Valid");
				}

				// IP addresses
				if (checkIp || settings.ProtocolSupport != IpSupport.IPv6)
				{
					try
					{
						var ipv4 = await ipAddressService.GetPublicIpAddressAsync(IpSupport.IPv4);
						if (!string.IsNullOrEmpty(ipv4))
						{
							output.WriteInfo($"Public IPv4: {ipv4}");
						}
					}
					catch (Exception ex)
					{
						output.WriteWarning($"IPv4: Unable to retrieve ({ex.Message})");
					}
				}

				if (checkIp || settings.ProtocolSupport != IpSupport.IPv4)
				{
					try
					{
						var ipv6 = await ipAddressService.GetPublicIpAddressAsync(IpSupport.IPv6);
						if (!string.IsNullOrEmpty(ipv6))
						{
							output.WriteInfo($"Public IPv6: {ipv6}");
						}
					}
					catch (Exception ex)
					{
						output.WriteWarning($"IPv6: Unable to retrieve ({ex.Message})");
					}
				}

				// Selected domains
				output.WriteInfo($"Selected domains: {settings.SelectedDomains.Count}");

				// Service status (if requested)
				if (checkService)
				{
					var serviceRunning = IsServiceRunning();
					if (serviceRunning)
					{
						output.WriteSuccess("DnsTube Service: Running");
					}
					else
					{
						output.WriteWarning("DnsTube Service: Not running");
					}
				}

				return 0;
			}
			catch (Exception ex)
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
				return 1;
			}
		}

		private static async Task<object> GetStatusDataAsync(ISettingsService settingsService, 
			IIpAddressService ipAddressService, bool checkIp, bool checkService, CliOptions options)
		{
			var settings = await settingsService.GetAsync();
			var validationError = settingsService.ValidateSettings(settings);

			string? ipv4 = null;
			string? ipv6 = null;

			if (checkIp)
			{
				try { ipv4 = await ipAddressService.GetPublicIpAddressAsync(IpSupport.IPv4); } catch { }
				try { ipv6 = await ipAddressService.GetPublicIpAddressAsync(IpSupport.IPv6); } catch { }
			}

			return new
			{
				success = true,
				timestamp = DateTime.UtcNow,
				data = new
				{
					configValid = validationError == null,
					validationError,
					ipv4Address = ipv4,
					ipv6Address = ipv6,
					selectedDomainsCount = settings.SelectedDomains.Count,
					serviceRunning = checkService ? IsServiceRunning() : (bool?)null,
					standaloneMode = options.IsStandaloneMode
				}
			};
		}

		private static bool IsServiceRunning()
		{
			try
			{
				var services = System.ServiceProcess.ServiceController.GetServices();
				var dnsTubeService = services.FirstOrDefault(s => 
					s.ServiceName.Contains("DnsTube", StringComparison.OrdinalIgnoreCase));
				return dnsTubeService != null && dnsTubeService.Status == System.ServiceProcess.ServiceControllerStatus.Running;
			}
			catch
			{
				return false;
			}
		}
	}
}