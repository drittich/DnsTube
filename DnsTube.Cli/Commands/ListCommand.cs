using System.CommandLine;
using DnsTube.Cli.Models;
using DnsTube.Cli.Services;
using DnsTube.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace DnsTube.Cli.Commands
{
	/// <summary>
	/// List command - Display zones, domains, and network adapters
	/// </summary>
	public static class ListCommand
	{
		public static Command Create(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("list", "List zones, domains, and adapters");

			// Add subcommands
			command.AddCommand(CreateZonesCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateDomainsCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateAdaptersCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));

			return command;
		}

		private static Command CreateZonesCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("zones", "List all Cloudflare zones");

			var showIdsOption = new Option<bool>(
				aliases: new[] { "--show-ids" },
				description: "Display zone IDs");

			command.AddOption(showIdsOption);

			command.SetHandler(async (bool showIds, string? configFile, string? configFileOnly,
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

				await ExecuteZonesAsync(options, showIds);
			},
			showIdsOption, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static Command CreateDomainsCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("domains", "List all DNS records");

			var selectedOnlyOption = new Option<bool>(
				aliases: new[] { "--selected-only" },
				description: "Show only selected domains");

			var zoneOption = new Option<string?>(
				aliases: new[] { "--zone", "-z" },
				description: "Filter by zone name");

			command.AddOption(selectedOnlyOption);
			command.AddOption(zoneOption);

			command.SetHandler(async (bool selectedOnly, string? zone, string? configFile, string? configFileOnly,
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

				await ExecuteDomainsAsync(options, selectedOnly, zone);
			},
			selectedOnlyOption, zoneOption, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static Command CreateAdaptersCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("adapters", "List available network adapters");

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

				await ExecuteAdaptersAsync(options);
			},
			configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteZonesAsync(CliOptions options, bool showIds)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				// Validate settings first
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

				// Get CloudflareService - need to cast to access ListZoneesAsync
				var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();
				var cloudflareServiceImpl = (DnsTube.Core.Services.CloudflareService)cloudflareService;

				var zones = await output.ShowProgressAsync("Fetching zones from Cloudflare...",
					async () => await cloudflareServiceImpl.ListZoneesAsync());

				if (options.JsonOutput)
				{
					var jsonData = zones.Select(z => new
					{
						id = showIds ? z.id : null,
						name = z.name,
						status = z.status,
						paused = z.paused,
						type = z.type
					}).ToList();

					output.WriteJson(new
					{
						success = true,
						timestamp = DateTime.UtcNow,
						count = zones.Count,
						zones = jsonData
					});
				}
				else
				{
					if (!zones.Any())
					{
						output.WriteInfo("No zones found");
						return 0;
					}

					output.WriteInfo($"Found {zones.Count} zone(s):");
					output.WriteLine();

					if (showIds)
					{
						var table = new Table();
						table.AddColumn(new TableColumn("Zone Name"));
						table.AddColumn(new TableColumn("Zone ID"));
						table.AddColumn(new TableColumn("Status"));

						foreach (var zone in zones.OrderBy(z => z.name))
						{
							table.AddRow(
								Markup.Escape(zone.name),
								Markup.Escape(zone.id),
								zone.paused ? "[red]Paused[/]" : "[green]Active[/]"
							);
						}

						AnsiConsole.Write(table);
					}
					else
					{
						foreach (var zone in zones.OrderBy(z => z.name))
						{
							var statusMark = zone.paused ? "⏸" : "✓";
							output.WriteInfo($"{statusMark} {zone.name}");
						}
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

		private static async Task<int> ExecuteDomainsAsync(CliOptions options, bool selectedOnly, string? zoneFilter)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();

				// Validate settings first
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

				var allRecords = await output.ShowProgressAsync("Fetching DNS records from Cloudflare...",
					async () => await cloudflareService.GetAllDnsRecordsByZoneAsync());

				// Apply filters
				var filteredRecords = allRecords.AsEnumerable();

				if (selectedOnly)
				{
					filteredRecords = filteredRecords.Where(r =>
						settings.SelectedDomains.Any(s =>
							s.ZoneName == r.zone_name &&
							s.DnsName == r.name &&
							s.Type == r.type));
				}

				if (!string.IsNullOrEmpty(zoneFilter))
				{
					filteredRecords = filteredRecords.Where(r =>
						r.zone_name.Contains(zoneFilter, StringComparison.OrdinalIgnoreCase));
				}

				var records = filteredRecords.OrderBy(r => r.zone_name).ThenBy(r => r.name).ThenBy(r => r.type).ToList();

				if (options.JsonOutput)
				{
					var jsonData = records.Select(r => new
					{
						zoneName = r.zone_name,
						name = r.name,
						type = r.type,
						content = r.content,
						ttl = r.ttl,
						proxied = r.proxied,
						selected = settings.SelectedDomains.Any(s =>
							s.ZoneName == r.zone_name &&
							s.DnsName == r.name &&
							s.Type == r.type)
					}).ToList();

					output.WriteJson(new
					{
						success = true,
						timestamp = DateTime.UtcNow,
						count = records.Count,
						domains = jsonData
					});
				}
				else
				{
					if (!records.Any())
					{
						output.WriteInfo("No DNS records found");
						return 0;
					}

					output.WriteInfo($"Found {records.Count} DNS record(s):");
					output.WriteLine();

					var table = new Table();
					table.AddColumn(new TableColumn("Selected"));
					table.AddColumn(new TableColumn("Zone"));
					table.AddColumn(new TableColumn("Name"));
					table.AddColumn(new TableColumn("Type"));
					table.AddColumn(new TableColumn("Content"));

					foreach (var record in records)
					{
						var isSelected = settings.SelectedDomains.Any(s =>
							s.ZoneName == record.zone_name &&
							s.DnsName == record.name &&
							s.Type == record.type);

						var selectedMark = isSelected ? "[green]✓[/]" : " ";
						var content = record.content.Length > 50
							? Markup.Escape(record.content.Substring(0, 47) + "...")
							: Markup.Escape(record.content);

						table.AddRow(
							selectedMark,
							Markup.Escape(record.zone_name),
							Markup.Escape(record.name),
							Markup.Escape(record.type),
							content
						);
					}

					AnsiConsole.Write(table);
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

		private static Task<int> ExecuteAdaptersAsync(CliOptions options)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var ipAddressService = serviceProvider.GetRequiredService<IIpAddressService>();

				var adapters = ipAddressService.GetNetworkAdapters();

				if (options.JsonOutput)
				{
					var jsonData = adapters.Select(a => new
					{
						name = a.Name,
						ipAddress = a.IpAddress
					}).ToList();

					output.WriteJson(new
					{
						success = true,
						timestamp = DateTime.UtcNow,
						count = adapters.Count,
						adapters = jsonData
					});
				}
				else
				{
					if (!adapters.Any())
					{
						output.WriteInfo("No network adapters found");
						return Task.FromResult(0);
					}

					output.WriteInfo($"Found {adapters.Count} network adapter(s):");
					output.WriteLine();

					foreach (var adapter in adapters.OrderBy(a => a.Name))
					{
						output.WriteInfo($"  {adapter.Name}: {adapter.IpAddress}");
					}
				}

				return Task.FromResult(0);
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
				return Task.FromResult(1);
			}
		}
	}
}