using System.CommandLine;
using DnsTube.Cli.Models;
using DnsTube.Cli.Services;
using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsTube.Cli.Commands
{
	/// <summary>
	/// Update command - Perform DNS update
	/// </summary>
	public static class UpdateCommand
	{
		public static Command Create(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("update", "Perform DNS update");

			var ipv4Option = new Option<bool>(
				aliases: new[] { "--ipv4" },
				description: "Update only IPv4 records");

			var ipv6Option = new Option<bool>(
				aliases: new[] { "--ipv6" },
				description: "Update only IPv6 records");

			var domainOption = new Option<string[]?>(
				aliases: new[] { "--domain", "-d" },
				description: "Update only specified domain(s). Can be specified multiple times.");

			var dryRunOption = new Option<bool>(
				aliases: new[] { "--dry-run" },
				description: "Show what would be updated without making changes");

			var forceOption = new Option<bool>(
				aliases: new[] { "--force", "-f" },
				description: "Force update even if IP hasn't changed");

			command.AddOption(ipv4Option);
			command.AddOption(ipv6Option);
			command.AddOption(domainOption);
			command.AddOption(dryRunOption);
			command.AddOption(forceOption);

			command.SetHandler(async (context) =>
			{
				var ipv4Only = context.ParseResult.GetValueForOption(ipv4Option);
				var ipv6Only = context.ParseResult.GetValueForOption(ipv6Option);
				var domains = context.ParseResult.GetValueForOption(domainOption);
				var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
				var force = context.ParseResult.GetValueForOption(forceOption);
				var configFile = context.ParseResult.GetValueForOption(configFileOption);
				var configFileOnly = context.ParseResult.GetValueForOption(configFileOnlyOption);
				var json = context.ParseResult.GetValueForOption(jsonOption);
				var verbose = context.ParseResult.GetValueForOption(verboseOption);
				var noColor = context.ParseResult.GetValueForOption(noColorOption);

				var options = new CliOptions
				{
					ConfigFile = configFile,
					ConfigFileOnly = configFileOnly,
					JsonOutput = json,
					Verbose = verbose,
					NoColor = noColor
				};

				context.ExitCode = await ExecuteAsync(options, ipv4Only, ipv6Only, domains, dryRun, force);
			});

			return command;
		}

		private static async Task<int> ExecuteAsync(CliOptions options, bool ipv4Only, bool ipv6Only, 
			string[]? domainFilter, bool dryRun, bool force)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var ipAddressService = serviceProvider.GetRequiredService<IIpAddressService>();
				var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();

				// Load settings
				var settings = await settingsService.GetAsync();
				var validationError = settingsService.ValidateSettings(settings);

				if (validationError != null)
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = $"Invalid configuration: {validationError}" });
					}
					else
					{
						output.WriteError($"Invalid configuration: {validationError}");
					}
					return 1;
				}

				// Determine which protocols to update
				IpSupport protocolToUpdate;
				if (ipv4Only && ipv6Only)
				{
					output.WriteError("Cannot specify both --ipv4 and --ipv6");
					return 1;
				}
				else if (ipv4Only)
				{
					protocolToUpdate = IpSupport.IPv4;
				}
				else if (ipv6Only)
				{
					protocolToUpdate = IpSupport.IPv6;
				}
				else
				{
					protocolToUpdate = settings.ProtocolSupport;
				}

				if (dryRun && !options.JsonOutput)
				{
					output.WriteWarning("DRY RUN MODE - No changes will be made");
					output.WriteLine();
				}

				var results = new List<UpdateResult>();

				// Update IPv4 if needed
				if (protocolToUpdate == IpSupport.IPv4 || protocolToUpdate == IpSupport.IPv4AndIPv6)
				{
					var result = await UpdateProtocolAsync(IpSupport.IPv4, serviceProvider, options, 
						domainFilter, dryRun, force);
					results.Add(result);
				}

				// Update IPv6 if needed
				if (protocolToUpdate == IpSupport.IPv6 || protocolToUpdate == IpSupport.IPv4AndIPv6)
				{
					var result = await UpdateProtocolAsync(IpSupport.IPv6, serviceProvider, options, 
						domainFilter, dryRun, force);
					results.Add(result);
				}

				// Output results
				if (options.JsonOutput)
				{
					output.WriteJson(new
					{
						success = results.All(r => r.Success),
						dryRun,
						timestamp = DateTime.UtcNow,
						results = results.Select(r => new
						{
							protocol = r.Protocol.ToString(),
							success = r.Success,
							ipAddress = r.IpAddress,
							updatedCount = r.UpdatedCount,
							skippedCount = r.SkippedCount,
							errorCount = r.ErrorCount,
							errors = r.Errors
						})
					});
				}
				else
				{
					output.WriteLine();
					var totalUpdated = results.Sum(r => r.UpdatedCount);
					var totalSkipped = results.Sum(r => r.SkippedCount);
					var totalErrors = results.Sum(r => r.ErrorCount);

					if (dryRun)
					{
						output.WriteInfo($"Would update {totalUpdated} record(s)");
						if (totalSkipped > 0)
						{
							output.WriteInfo($"Would skip {totalSkipped} record(s) (already up to date)");
						}
					}
					else
					{
						if (totalUpdated > 0)
						{
							output.WriteSuccess($"Updated {totalUpdated} record(s)");
						}
						else
						{
							output.WriteInfo("No records needed updating");
						}

						if (totalSkipped > 0)
						{
							output.WriteInfo($"Skipped {totalSkipped} record(s) (already up to date)");
						}
					}

					if (totalErrors > 0)
					{
						output.WriteError($"Failed to update {totalErrors} record(s)");
						foreach (var result in results.Where(r => r.Errors.Any()))
						{
							foreach (var error in result.Errors)
							{
								output.WriteError($"  {error}");
							}
						}
					}
				}

				return results.All(r => r.Success) ? 0 : 1;
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

		private static async Task<UpdateResult> UpdateProtocolAsync(IpSupport protocol, 
			ServiceProvider serviceProvider, CliOptions options, string[]? domainFilter, 
			bool dryRun, bool force)
		{
			var output = serviceProvider.GetRequiredService<IOutputService>();
			var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
			var ipAddressService = serviceProvider.GetRequiredService<IIpAddressService>();
			var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();
			var logService = serviceProvider.GetRequiredService<ILogService>();

			var result = new UpdateResult { Protocol = protocol };

			try
			{
				// Get current public IP
				var publicIp = await ipAddressService.GetPublicIpAddressAsync(protocol);
				if (string.IsNullOrEmpty(publicIp))
				{
					result.Errors.Add($"Unable to retrieve public {protocol} address");
					return result;
				}

				result.IpAddress = publicIp;

				if (!options.JsonOutput && options.Verbose)
				{
					output.WriteInfo($"Current {protocol} address: {publicIp}");
				}

				// Get settings
				var settings = await settingsService.GetAsync();

				// Check if update is needed (unless forced)
				if (!force && !dryRun)
				{
					var previousIp = protocol == IpSupport.IPv4 
						? settings.PublicIpv4Address 
						: settings.PublicIpv6Address;

					if (publicIp == previousIp)
					{
						if (!options.JsonOutput && options.Verbose)
						{
							output.WriteInfo($"{protocol} address unchanged, skipping update");
						}
						result.Success = true;
						return result;
					}
				}

				// Get all DNS records
				var allRecords = await cloudflareService.GetAllDnsRecordsByZoneAsync();
				
				// Determine record types to update
				var recordType = protocol == IpSupport.IPv4 ? "A" : "AAAA";
				var typesToUpdate = new List<string> { recordType, "SPF", "TXT" };

				// Filter records based on selected domains and protocol
				var recordsToUpdate = allRecords.Where(r =>
					settings.SelectedDomains.Any(s =>
						s.ZoneName == r.zone_name &&
						s.DnsName == r.name &&
						s.Type == r.type) &&
					typesToUpdate.Contains(r.type)
				).ToList();

				// Apply domain filter if specified
				if (domainFilter != null && domainFilter.Length > 0)
				{
					recordsToUpdate = recordsToUpdate.Where(r =>
						domainFilter.Any(d => r.name.Contains(d, StringComparison.OrdinalIgnoreCase))
					).ToList();
				}

				if (!recordsToUpdate.Any())
				{
					if (!options.JsonOutput && options.Verbose)
					{
						output.WriteInfo($"No {protocol} records to update");
					}
					result.Success = true;
					return result;
				}

				// Process each record
				foreach (var record in recordsToUpdate)
				{
					try
					{
						// Find matching domain configuration
						var domainConfig = settings.SelectedDomains.FirstOrDefault(s =>
							s.ZoneName == record.zone_name &&
							s.DnsName == record.name &&
							s.Type == record.type);

						if (domainConfig == null)
							continue;

						// Resolve IP per-record based on configuration
						var ipAddress = await ipAddressService.GetIpAddressForRecord(
							domainConfig, protocol, publicIp);

						if (ipAddress == null)
						{
							result.Errors.Add($"No IP available for {record.type} record [{record.name}]");
							result.ErrorCount++;
							continue;
						}

						// Determine new content
						string newContent;
						if (record.type == "SPF" || record.type == "TXT")
						{
							newContent = UpdateDnsRecordContent(protocol, record.content, ipAddress);
						}
						else
						{
							newContent = ipAddress;
						}

						// Check if update is needed
						if (record.content == newContent && !force)
						{
							result.SkippedCount++;
							if (!options.JsonOutput && options.Verbose)
							{
								output.WriteInfo($"Skipping {record.type} record [{record.name}] - already up to date");
							}
							continue;
						}

						if (dryRun)
						{
							result.UpdatedCount++;
							if (!options.JsonOutput)
							{
								var source = string.IsNullOrWhiteSpace(domainConfig.NetworkAdapterName) ||
									domainConfig.NetworkAdapterName == "_PUBLIC_"
									? "public IP"
									: $"adapter '{domainConfig.NetworkAdapterName}'";
								
								output.WriteInfo($"Would update {record.type} record [{record.name}] to {newContent} from {source}");
							}
						}
						else
						{
							// Perform the update - we need to call the method directly since it's not in interface
							var cloudflareServiceImpl = (DnsTube.Core.Services.CloudflareService)cloudflareService;
							await cloudflareServiceImpl.UpdateDnsAsync(
								protocol, record.zone_id, record.id, record.type,
								record.name, newContent, record.ttl, record.proxied);

							result.UpdatedCount++;

							if (!options.JsonOutput)
							{
								var source = string.IsNullOrWhiteSpace(domainConfig.NetworkAdapterName) ||
									domainConfig.NetworkAdapterName == "_PUBLIC_"
									? "public IP"
									: $"adapter '{domainConfig.NetworkAdapterName}'";

								output.WriteSuccess($"Updated {record.type} record [{record.name}] to {newContent} from {source}");
							}
						}
					}
					catch (Exception ex)
					{
						result.ErrorCount++;
						result.Errors.Add($"Error updating {record.type} record [{record.name}]: {ex.Message}");
						
						if (!options.JsonOutput)
						{
							output.WriteError($"Error updating {record.type} record [{record.name}]: {ex.Message}");
						}
					}
				}

				result.Success = result.ErrorCount == 0;
				return result;
			}
			catch (Exception ex)
			{
				result.Errors.Add(ex.Message);
				return result;
			}
		}

		private static string UpdateDnsRecordContent(IpSupport protocol, string content, string ipAddress)
		{
			var ipv4Regex = @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b";
			var ipv6Regex = @"\b(?:[A-Fa-f0-9]{1,4}:){7}[A-Fa-f0-9]{1,4}\b";

			if (protocol == IpSupport.IPv4)
			{
				return System.Text.RegularExpressions.Regex.Replace(content, ipv4Regex, ipAddress);
			}
			else if (protocol == IpSupport.IPv6)
			{
				return System.Text.RegularExpressions.Regex.Replace(content, ipv6Regex, ipAddress);
			}

			return content;
		}

		private class UpdateResult
		{
			public IpSupport Protocol { get; set; }
			public bool Success { get; set; }
			public string? IpAddress { get; set; }
			public int UpdatedCount { get; set; }
			public int SkippedCount { get; set; }
			public int ErrorCount { get; set; }
			public List<string> Errors { get; set; } = new();
		}
	}
}