using System.CommandLine;
using DnsTube.Cli.Models;
using DnsTube.Cli.Services;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace DnsTube.Cli.Commands
{
	/// <summary>
	/// Domains command - Manage selected domains for DNS updates
	/// </summary>
	public static class DomainsCommand
	{
		public static Command Create(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("domains", "Manage selected domains");

			// Add subcommands
			command.AddCommand(CreateListCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateAddCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateRemoveCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateSelectCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateClearCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));

			return command;
		}

		private static Command CreateListCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("list", "List all available DNS records with selection indicators");

			var zoneOption = new Option<string?>(
				aliases: new[] { "--zone", "-z" },
				description: "Filter by zone name");

			var typeOption = new Option<string?>(
				aliases: new[] { "--type", "-t" },
				description: "Filter by record type (A, AAAA, TXT, etc.)");

			var selectedOnlyOption = new Option<bool>(
				aliases: new[] { "--selected-only" },
				description: "Show only selected domains");

			command.AddOption(zoneOption);
			command.AddOption(typeOption);
			command.AddOption(selectedOnlyOption);

			command.SetHandler(async (string? zone, string? type, bool selectedOnly,
				string? configFile, string? configFileOnly, bool json, bool verbose, bool noColor) =>
			{
				var options = new CliOptions
				{
					ConfigFile = configFile,
					ConfigFileOnly = configFileOnly,
					JsonOutput = json,
					Verbose = verbose,
					NoColor = noColor
				};

				await ExecuteListAsync(options, zone, type, selectedOnly);
			},
			zoneOption, typeOption, selectedOnlyOption,
			configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static Command CreateAddCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("add", "Add domain(s) to selected list");

			var zoneOption = new Option<string?>(
				aliases: new[] { "--zone", "-z" },
				description: "Zone name");

			var nameOption = new Option<string?>(
				aliases: new[] { "--name", "-n" },
				description: "Domain name");

			var typeOption = new Option<string?>(
				aliases: new[] { "--type", "-t" },
				description: "Record type (A, AAAA, TXT, etc.)");

			var adapterOption = new Option<string?>(
				aliases: new[] { "--adapter", "-a" },
				description: "Network adapter name (_PUBLIC_, _DEFAULT_, or adapter name)");

			command.AddOption(zoneOption);
			command.AddOption(nameOption);
			command.AddOption(typeOption);
			command.AddOption(adapterOption);

			command.SetHandler(async (context) =>
			{
				var zone = context.ParseResult.GetValueForOption(zoneOption);
				var name = context.ParseResult.GetValueForOption(nameOption);
				var type = context.ParseResult.GetValueForOption(typeOption);
				var adapter = context.ParseResult.GetValueForOption(adapterOption);
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

				context.ExitCode = await ExecuteAddAsync(options, zone, name, type, adapter);
			});

			return command;
		}

		private static Command CreateRemoveCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("remove", "Remove domain(s) from selected list");

			var zoneOption = new Option<string?>(
				aliases: new[] { "--zone", "-z" },
				description: "Zone name");

			var nameOption = new Option<string?>(
				aliases: new[] { "--name", "-n" },
				description: "Domain name");

			var typeOption = new Option<string?>(
				aliases: new[] { "--type", "-t" },
				description: "Record type (A, AAAA, TXT, etc.)");

			command.AddOption(zoneOption);
			command.AddOption(nameOption);
			command.AddOption(typeOption);

			command.SetHandler(async (string? zone, string? name, string? type,
				string? configFile, string? configFileOnly, bool json, bool verbose, bool noColor) =>
			{
				var options = new CliOptions
				{
					ConfigFile = configFile,
					ConfigFileOnly = configFileOnly,
					JsonOutput = json,
					Verbose = verbose,
					NoColor = noColor
				};

				await ExecuteRemoveAsync(options, zone, name, type);
			},
			zoneOption, nameOption, typeOption,
			configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static Command CreateSelectCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("select", "Interactive multi-select interface for domains");

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

				await ExecuteSelectAsync(options);
			},
			configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static Command CreateClearCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("clear", "Remove all selected domains");

			var confirmOption = new Option<bool>(
				aliases: new[] { "--confirm", "-y" },
				description: "Skip confirmation prompt");

			command.AddOption(confirmOption);

			command.SetHandler(async (bool confirm, string? configFile, string? configFileOnly,
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

				await ExecuteClearAsync(options, confirm);
			},
			confirmOption, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		// ========== List Implementation ==========
		private static async Task<int> ExecuteListAsync(CliOptions options, string? zoneFilter, string? typeFilter, bool selectedOnly)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();

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

				if (!string.IsNullOrEmpty(typeFilter))
				{
					filteredRecords = filteredRecords.Where(r =>
						r.type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase));
				}

				var records = filteredRecords.OrderBy(r => r.zone_name).ThenBy(r => r.name).ThenBy(r => r.type).ToList();

				if (options.JsonOutput)
				{
					var jsonData = records.Select(r =>
					{
						var selectedDomain = settings.SelectedDomains.FirstOrDefault(s =>
							s.ZoneName == r.zone_name &&
							s.DnsName == r.name &&
							s.Type == r.type);

						return new
						{
							zoneName = r.zone_name,
							name = r.name,
							type = r.type,
							content = r.content,
							ttl = r.ttl,
							proxied = r.proxied,
							selected = selectedDomain != null,
							adapter = selectedDomain?.NetworkAdapterName
						};
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
					table.AddColumn(new TableColumn("Adapter"));

					foreach (var record in records)
					{
						var selectedDomain = settings.SelectedDomains.FirstOrDefault(s =>
							s.ZoneName == record.zone_name &&
							s.DnsName == record.name &&
							s.Type == record.type);

						var selectedMark = selectedDomain != null ? "[green]✓[/]" : " ";
						var content = record.content.Length > 30
							? Markup.Escape(record.content.Substring(0, 27) + "...")
							: Markup.Escape(record.content);

						var adapter = selectedDomain?.NetworkAdapterName ?? "-";

						table.AddRow(
							selectedMark,
							Markup.Escape(record.zone_name),
							Markup.Escape(record.name),
							Markup.Escape(record.type),
							content,
							Markup.Escape(adapter)
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

		// ========== Add Implementation ==========
		private static async Task<int> ExecuteAddAsync(CliOptions options, string? zone, string? name, string? type, string? adapter)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();
				var ipAddressService = serviceProvider.GetRequiredService<IIpAddressService>();

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

				// If no options provided, use interactive mode
				if (string.IsNullOrEmpty(zone) && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(type))
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = "Interactive mode not available in JSON output mode. Please specify --zone, --name, and --type." });
						return 1;
					}

					return await ExecuteAddInteractiveAsync(serviceProvider, output, settingsService, cloudflareService, ipAddressService);
				}

				// Validate required parameters for non-interactive mode
				if (string.IsNullOrEmpty(zone) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = "Missing required parameters. Specify --zone, --name, and --type." });
					}
					else
					{
						output.WriteError("Missing required parameters. Specify --zone, --name, and --type.");
					}
					return 1;
				}

				// Validate domain exists in Cloudflare
				var allRecords = await output.ShowProgressAsync("Fetching DNS records from Cloudflare...",
					async () => await cloudflareService.GetAllDnsRecordsByZoneAsync());

				var record = allRecords.FirstOrDefault(r =>
					r.zone_name.Equals(zone, StringComparison.OrdinalIgnoreCase) &&
					r.name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
					r.type.Equals(type, StringComparison.OrdinalIgnoreCase));

				if (record == null)
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = $"Domain not found: {name} ({type}) in zone {zone}" });
					}
					else
					{
						output.WriteError($"Domain not found: {name} ({type}) in zone {zone}");
					}
					return 1;
				}

				// Check if already selected
				var alreadySelected = settings.SelectedDomains.Any(s =>
					s.ZoneName == record.zone_name &&
					s.DnsName == record.name &&
					s.Type == record.type);

				if (alreadySelected)
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = "Domain is already selected" });
					}
					else
					{
						output.WriteWarning("Domain is already selected");
					}
					return 1;
				}

				// Add the domain
				var selectedDomains = settings.SelectedDomains.ToList();
				selectedDomains.Add(new SelectedDomain
				{
					ZoneName = record.zone_name,
					DnsName = record.name,
					Type = record.type,
					NetworkAdapterName = adapter ?? "_PUBLIC_"
				});

				await settingsService.SaveDomainsAsync(selectedDomains);

				if (options.JsonOutput)
				{
					output.WriteJson(new
					{
						success = true,
						message = "Domain added successfully",
						domain = new
						{
							zoneName = record.zone_name,
							name = record.name,
							type = record.type,
							adapter = adapter ?? "_PUBLIC_"
						}
					});
				}
				else
				{
					output.WriteSuccess($"Added: {record.name} ({record.type}) in {record.zone_name}");
					output.WriteInfo($"Network adapter: {adapter ?? "_PUBLIC_"}");
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

		private static async Task<int> ExecuteAddInteractiveAsync(IServiceProvider serviceProvider,
			IOutputService output, ISettingsService settingsService,
			ICloudflareService cloudflareService, IIpAddressService ipAddressService)
		{
			var settings = await settingsService.GetAsync();

			var allRecords = await output.ShowProgressAsync("Fetching DNS records from Cloudflare...",
				async () => await cloudflareService.GetAllDnsRecordsByZoneAsync());

			// Filter out already selected domains
			var availableRecords = allRecords.Where(r =>
				!settings.SelectedDomains.Any(s =>
					s.ZoneName == r.zone_name &&
					s.DnsName == r.name &&
					s.Type == r.type)).ToList();

			if (!availableRecords.Any())
			{
				output.WriteWarning("No available domains to add (all domains are already selected)");
				return 0;
			}

			// Create choices grouped by zone
			var choices = availableRecords
				.OrderBy(r => r.zone_name)
				.ThenBy(r => r.name)
				.ThenBy(r => r.type)
				.Select(r => $"{r.zone_name} | {r.name} ({r.type})")
				.ToList();

			var selected = AnsiConsole.Prompt(
				new MultiSelectionPrompt<string>()
					.Title("Select domains to [green]add[/]:")
					.PageSize(10)
					.MoreChoicesText("[grey](Move up and down to reveal more domains)[/]")
					.InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
					.AddChoices(choices));

			if (!selected.Any())
			{
				output.WriteWarning("No domains selected");
				return 0;
			}

			// Configure adapters for each selected domain
			var selectedDomains = settings.SelectedDomains.ToList();
			var adapters = ipAddressService.GetNetworkAdapters();
			var adapterChoices = new List<string> { "_PUBLIC_", "_DEFAULT_" };
			adapterChoices.AddRange(adapters.Select(a => a.Name));

			foreach (var choice in selected)
			{
				var parts = choice.Split(" | ");
				var zoneName = parts[0];
				var namePart = parts[1];
				var nameAndType = namePart.Split(" (");
				var dnsName = nameAndType[0];
				var recordType = nameAndType[1].TrimEnd(')');

				output.WriteLine();
				output.WriteInfo($"Configuring: {dnsName} ({recordType})");

				var adapterName = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title($"Select network adapter for [yellow]{dnsName}[/]:")
						.PageSize(10)
						.AddChoices(adapterChoices));

				selectedDomains.Add(new SelectedDomain
				{
					ZoneName = zoneName,
					DnsName = dnsName,
					Type = recordType,
					NetworkAdapterName = adapterName
				});
			}

			await settingsService.SaveDomainsAsync(selectedDomains);

			output.WriteLine();
			output.WriteSuccess($"Added {selected.Count} domain(s) successfully");

			return 0;
		}

		// ========== Remove Implementation ==========
		private static async Task<int> ExecuteRemoveAsync(CliOptions options, string? zone, string? name, string? type)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				var settings = await settingsService.GetAsync();

				if (!settings.SelectedDomains.Any())
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = "No selected domains to remove" });
					}
					else
					{
						output.WriteWarning("No selected domains to remove");
					}
					return 0;
				}

				// If no options provided, use interactive mode
				if (string.IsNullOrEmpty(zone) && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(type))
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = "Interactive mode not available in JSON output mode. Please specify --zone, --name, and/or --type." });
						return 1;
					}

					return await ExecuteRemoveInteractiveAsync(serviceProvider, output, settingsService);
				}

				// Filter selected domains based on provided criteria
				var domainsToRemove = settings.SelectedDomains.AsEnumerable();

				if (!string.IsNullOrEmpty(zone))
				{
					domainsToRemove = domainsToRemove.Where(d =>
						d.ZoneName.Contains(zone, StringComparison.OrdinalIgnoreCase));
				}

				if (!string.IsNullOrEmpty(name))
				{
					domainsToRemove = domainsToRemove.Where(d =>
						d.DnsName.Contains(name, StringComparison.OrdinalIgnoreCase));
				}

				if (!string.IsNullOrEmpty(type))
				{
					domainsToRemove = domainsToRemove.Where(d =>
						d.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
				}

				var domainsToRemoveList = domainsToRemove.ToList();

				if (!domainsToRemoveList.Any())
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = "No matching domains found to remove" });
					}
					else
					{
						output.WriteWarning("No matching domains found to remove");
					}
					return 0;
				}

				// Remove the domains
				var remainingDomains = settings.SelectedDomains
					.Except(domainsToRemoveList)
					.ToList();

				await settingsService.SaveDomainsAsync(remainingDomains);

				if (options.JsonOutput)
				{
					output.WriteJson(new
					{
						success = true,
						message = $"Removed {domainsToRemoveList.Count} domain(s)",
						removedCount = domainsToRemoveList.Count
					});
				}
				else
				{
					output.WriteSuccess($"Removed {domainsToRemoveList.Count} domain(s)");
					foreach (var domain in domainsToRemoveList)
					{
						output.WriteInfo($"  - {domain.DnsName} ({domain.Type}) in {domain.ZoneName}");
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

		private static async Task<int> ExecuteRemoveInteractiveAsync(IServiceProvider serviceProvider,
			IOutputService output, ISettingsService settingsService)
		{
			var settings = await settingsService.GetAsync();

			// Create choices for removal
			var choices = settings.SelectedDomains
				.OrderBy(d => d.ZoneName)
				.ThenBy(d => d.DnsName)
				.ThenBy(d => d.Type)
				.Select(d => $"{d.ZoneName} | {d.DnsName} ({d.Type}) - Adapter: {d.NetworkAdapterName ?? "_PUBLIC_"}")
				.ToList();

			var selected = AnsiConsole.Prompt(
				new MultiSelectionPrompt<string>()
					.Title("Select domains to [red]remove[/]:")
					.PageSize(10)
					.MoreChoicesText("[grey](Move up and down to reveal more domains)[/]")
					.InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
					.AddChoices(choices));

			if (!selected.Any())
			{
				output.WriteWarning("No domains selected for removal");
				return 0;
			}

			// Parse selections and remove them
			var domainsToRemove = new List<SelectedDomain>();
			foreach (var choice in selected)
			{
				var parts = choice.Split(" | ");
				var zoneName = parts[0];
				var rest = parts[1].Split(" - Adapter: ");
				var nameAndType = rest[0].Split(" (");
				var dnsName = nameAndType[0];
				var recordType = nameAndType[1].TrimEnd(')');

				var domain = settings.SelectedDomains.First(d =>
					d.ZoneName == zoneName &&
					d.DnsName == dnsName &&
					d.Type == recordType);

				domainsToRemove.Add(domain);
			}

			var remainingDomains = settings.SelectedDomains
				.Except(domainsToRemove)
				.ToList();

			await settingsService.SaveDomainsAsync(remainingDomains);

			output.WriteLine();
			output.WriteSuccess($"Removed {domainsToRemove.Count} domain(s) successfully");

			return 0;
		}

		// ========== Select Implementation ==========
		private static async Task<int> ExecuteSelectAsync(CliOptions options)
		{
			try
			{
				if (options.JsonOutput)
				{
					var sp = Program.BuildServiceProvider(options);
					var outputSvc = sp.GetRequiredService<IOutputService>();
					outputSvc.WriteJson(new { success = false, error = "Interactive selection mode not available in JSON output mode" });
					return 1;
				}

				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
				var cloudflareService = serviceProvider.GetRequiredService<ICloudflareService>();
				var ipAddressService = serviceProvider.GetRequiredService<IIpAddressService>();

				var settings = await settingsService.GetAsync();
				var validationError = settingsService.ValidateSettings(settings);

				if (validationError != null)
				{
					output.WriteError($"Invalid configuration: {validationError}");
					return 1;
				}

				var allRecords = await output.ShowProgressAsync("Fetching DNS records from Cloudflare...",
					async () => await cloudflareService.GetAllDnsRecordsByZoneAsync());

				// Group records by zone
				var recordsByZone = allRecords
					.GroupBy(r => r.zone_name)
					.OrderBy(g => g.Key)
					.ToList();

				// Create choices with zone grouping
				var choices = new List<string>();
				foreach (var zoneGroup in recordsByZone)
				{
					choices.Add($"[Zone: {zoneGroup.Key}]");
					foreach (var record in zoneGroup.OrderBy(r => r.name).ThenBy(r => r.type))
					{
						choices.Add($"  {record.name} ({record.type})");
					}
				}

				// Pre-select currently selected domains
				var currentlySelected = new List<string>();
				foreach (var selectedDomain in settings.SelectedDomains)
				{
					var matchingChoice = choices.FirstOrDefault(c =>
						c.Contains($"  {selectedDomain.DnsName} ({selectedDomain.Type})") &&
						choices[choices.IndexOf(c) > 0 ? choices.IndexOf(c) - 1 : 0].Contains($"[Zone: {selectedDomain.ZoneName}]"));

					if (matchingChoice != null)
					{
						currentlySelected.Add(matchingChoice);
					}
				}

				var prompt = new MultiSelectionPrompt<string>()
					.Title("Select domains to manage:")
					.PageSize(15)
					.MoreChoicesText("[grey](Move up and down to reveal more domains)[/]")
					.InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
					.AddChoices(choices);

				// Add default selections
				foreach (var selected in currentlySelected)
				{
					prompt = prompt.Select(selected);
				}

				var selectedChoices = AnsiConsole.Prompt(prompt);

				// Parse selections
				var newSelectedDomains = new List<SelectedDomain>();
				string? currentZone = null;

				for (int i = 0; i < choices.Count; i++)
				{
					var choice = choices[i];
					if (choice.StartsWith("[Zone:"))
					{
						currentZone = choice.Replace("[Zone: ", "").TrimEnd(']');
					}
					else if (selectedChoices.Contains(choice) && currentZone != null)
					{
						var trimmed = choice.Trim();
						var parts = trimmed.Split(" (");
						var dnsName = parts[0];
						var recordType = parts[1].TrimEnd(')');

						// Check if this domain had a previous adapter setting
						var existingDomain = settings.SelectedDomains.FirstOrDefault(d =>
							d.ZoneName == currentZone &&
							d.DnsName == dnsName &&
							d.Type == recordType);

						newSelectedDomains.Add(new SelectedDomain
						{
							ZoneName = currentZone,
							DnsName = dnsName,
							Type = recordType,
							NetworkAdapterName = existingDomain?.NetworkAdapterName ?? "_PUBLIC_"
						});
					}
				}

				// Configure adapters for newly added domains
				var adapters = ipAddressService.GetNetworkAdapters();
				var adapterChoices = new List<string> { "_PUBLIC_", "_DEFAULT_" };
				adapterChoices.AddRange(adapters.Select(a => a.Name));

				var newlyAdded = newSelectedDomains.Where(nd =>
					!settings.SelectedDomains.Any(od =>
						od.ZoneName == nd.ZoneName &&
						od.DnsName == nd.DnsName &&
						od.Type == nd.Type)).ToList();

				if (newlyAdded.Any())
				{
					output.WriteLine();
					output.WriteInfo($"Configuring {newlyAdded.Count} newly added domain(s)...");

					foreach (var domain in newlyAdded)
					{
						output.WriteLine();
						var adapterName = AnsiConsole.Prompt(
							new SelectionPrompt<string>()
								.Title($"Select network adapter for [yellow]{domain.DnsName}[/]:")
								.PageSize(10)
								.AddChoices(adapterChoices));

						domain.NetworkAdapterName = adapterName;
					}
				}

				await settingsService.SaveDomainsAsync(newSelectedDomains);

				output.WriteLine();
				output.WriteSuccess($"Selection updated: {newSelectedDomains.Count} domain(s) selected");

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

		// ========== Clear Implementation ==========
		private static async Task<int> ExecuteClearAsync(CliOptions options, bool confirm)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				var settings = await settingsService.GetAsync();

				if (!settings.SelectedDomains.Any())
				{
					if (options.JsonOutput)
					{
						output.WriteJson(new { success = false, error = "No selected domains to clear" });
					}
					else
					{
						output.WriteWarning("No selected domains to clear");
					}
					return 0;
				}

				var count = settings.SelectedDomains.Count;

				// Ask for confirmation unless --confirm flag is set
				if (!confirm && !options.JsonOutput)
				{
					var confirmed = AnsiConsole.Confirm(
						$"Are you sure you want to remove all {count} selected domain(s)?",
						false);

					if (!confirmed)
					{
						output.WriteWarning("Operation cancelled");
						return 0;
					}
				}

				await settingsService.RemoveAllSelectedDomainsAsync();

				if (options.JsonOutput)
				{
					output.WriteJson(new
					{
						success = true,
						message = "All selected domains cleared",
						clearedCount = count
					});
				}
				else
				{
					output.WriteSuccess($"Cleared {count} selected domain(s)");
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
	}
}