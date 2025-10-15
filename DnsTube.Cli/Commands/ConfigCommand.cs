using System.CommandLine;
using System.Text.Json;
using DnsTube.Cli.Models;
using DnsTube.Cli.Services;
using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace DnsTube.Cli.Commands
{
	/// <summary>
	/// Config command - Manage configuration settings
	/// </summary>
	public static class ConfigCommand
	{
		public static Command Create(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("config", "Manage configuration settings");

			// Add subcommands
			command.AddCommand(CreateShowCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateSetCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateWizardCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateValidateCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));
			command.AddCommand(CreateExportCommand(configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption));

			return command;
		}

		#region config show

		private static Command CreateShowCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("show", "Display current configuration");

			var revealSecretsOption = new Option<bool>(
				aliases: new[] { "--reveal-secrets" },
				description: "Show API key/token (default: masked)");

			command.AddOption(revealSecretsOption);

			command.SetHandler(async (bool revealSecrets, string? configFile, string? configFileOnly,
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

				await ExecuteShowAsync(options, revealSecrets);
			},
			revealSecretsOption, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteShowAsync(CliOptions options, bool revealSecrets)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				var settings = await settingsService.GetAsync();

				if (options.JsonOutput)
				{
					var data = new
					{
						success = true,
						data = new
						{
							emailAddress = settings.EmailAddress,
							isUsingToken = settings.IsUsingToken,
							apiKeyOrToken = revealSecrets ? settings.ApiKeyOrToken : MaskSecret(settings.ApiKeyOrToken),
							updateIntervalMinutes = settings.UpdateIntervalMinutes,
							protocolSupport = settings.ProtocolSupport.ToString(),
							ipv4Api = settings.IPv4_API,
							ipv6Api = settings.IPv6_API,
							selectedDomainsCount = settings.SelectedDomains.Count
						}
					};
					output.WriteJson(data);
					return 0;
				}

				output.WriteInfo("Current Configuration");
				output.WriteLine();

				output.WriteInfo($"Email Address: {settings.EmailAddress}");
				output.WriteInfo($"Authentication: {(settings.IsUsingToken ? "API Token" : "API Key")}");
				output.WriteInfo($"API Key/Token: {(revealSecrets ? settings.ApiKeyOrToken : MaskSecret(settings.ApiKeyOrToken))}");
				output.WriteInfo($"Update Interval: {settings.UpdateIntervalMinutes} minutes");
				output.WriteInfo($"Protocol Support: {settings.ProtocolSupport}");
				output.WriteInfo($"IPv4 API: {settings.IPv4_API}");
				output.WriteInfo($"IPv6 API: {settings.IPv6_API}");
				output.WriteInfo($"Selected Domains: {settings.SelectedDomains.Count}");

				return 0;
			}
			catch (Exception ex)
			{
				return HandleError(options, ex);
			}
		}

		#endregion

		#region config set

		private static Command CreateSetCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("set", "Update specific settings");

			var emailOption = new Option<string?>(
				aliases: new[] { "--email" },
				description: "Email address");

			var tokenOption = new Option<string?>(
				aliases: new[] { "--token" },
				description: "API token");

			var apiKeyOption = new Option<string?>(
				aliases: new[] { "--api-key" },
				description: "API key");

			var intervalOption = new Option<int?>(
				aliases: new[] { "--interval" },
				description: "Update interval in minutes");

			var ipv4ApiOption = new Option<string?>(
				aliases: new[] { "--ipv4-api" },
				description: "IPv4 API URL");

			var ipv6ApiOption = new Option<string?>(
				aliases: new[] { "--ipv6-api" },
				description: "IPv6 API URL");

			var protocolOption = new Option<string?>(
				aliases: new[] { "--protocol" },
				description: "Protocol support (IPv4, IPv6, or Both)");
	
			command.AddOption(emailOption);
			command.AddOption(tokenOption);
			command.AddOption(apiKeyOption);
			command.AddOption(intervalOption);
			command.AddOption(ipv4ApiOption);
			command.AddOption(ipv6ApiOption);
			command.AddOption(protocolOption);
	
			command.SetHandler(async (context) =>
			{
				var email = context.ParseResult.GetValueForOption(emailOption);
				var token = context.ParseResult.GetValueForOption(tokenOption);
				var apiKey = context.ParseResult.GetValueForOption(apiKeyOption);
				var interval = context.ParseResult.GetValueForOption(intervalOption);
				var ipv4Api = context.ParseResult.GetValueForOption(ipv4ApiOption);
				var ipv6Api = context.ParseResult.GetValueForOption(ipv6ApiOption);
				var protocol = context.ParseResult.GetValueForOption(protocolOption);
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
		
				await ExecuteSetAsync(options, email, token, apiKey, interval, ipv4Api, ipv6Api, protocol);
			});

			return command;
		}

		private static async Task<int> ExecuteSetAsync(CliOptions options, string? email, string? token,
			string? apiKey, int? interval, string? ipv4Api, string? ipv6Api, string? protocol)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				var settings = await settingsService.GetAsync();
				var changes = new List<string>();

				// Update settings
				if (email != null)
				{
					if (!email.Contains('@'))
					{
						output.WriteError("Invalid email address");
						return 1;
					}
					settings.EmailAddress = email;
					changes.Add($"Email: {email}");
				}

				if (token != null && apiKey != null)
				{
					output.WriteError("Cannot specify both --token and --api-key");
					return 1;
				}

				if (token != null)
				{
					settings.IsUsingToken = true;
					settings.ApiKeyOrToken = token;
					changes.Add("Authentication: API Token");
				}

				if (apiKey != null)
				{
					settings.IsUsingToken = false;
					settings.ApiKeyOrToken = apiKey;
					changes.Add("Authentication: API Key");
				}

				if (interval.HasValue)
				{
					if (interval.Value < 1)
					{
						output.WriteError("Interval must be at least 1 minute");
						return 1;
					}
					settings.UpdateIntervalMinutes = interval.Value;
					changes.Add($"Interval: {interval.Value} minutes");
				}

				if (ipv4Api != null)
				{
					if (!Uri.TryCreate(ipv4Api, UriKind.Absolute, out _))
					{
						output.WriteError("Invalid IPv4 API URL");
						return 1;
					}
					settings.IPv4_API = ipv4Api;
					changes.Add($"IPv4 API: {ipv4Api}");
				}

				if (ipv6Api != null)
				{
					if (!Uri.TryCreate(ipv6Api, UriKind.Absolute, out _))
					{
						output.WriteError("Invalid IPv6 API URL");
						return 1;
					}
					settings.IPv6_API = ipv6Api;
					changes.Add($"IPv6 API: {ipv6Api}");
				}

				if (protocol != null)
				{
					if (Enum.TryParse<IpSupport>(protocol, true, out var protocolValue))
					{
						settings.ProtocolSupport = protocolValue;
						changes.Add($"Protocol: {protocolValue}");
					}
					else
					{
						output.WriteError("Invalid protocol. Must be IPv4, IPv6, or Both");
						return 1;
					}
				}
		
				if (changes.Count == 0)
				{
					output.WriteWarning("No changes specified");
					return 0;
				}

				// Save changes
				await settingsService.SaveAsync(settings);

				if (options.JsonOutput)
				{
					output.WriteJson(new { success = true, changes });
				}
				else
				{
					output.WriteSuccess("Configuration updated:");
					foreach (var change in changes)
					{
						output.WriteInfo($"  • {change}");
					}
				}

				return 0;
			}
			catch (Exception ex)
			{
				return HandleError(options, ex);
			}
		}

		#endregion

		#region config wizard

		private static Command CreateWizardCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("wizard", "Interactive configuration wizard");

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

				await ExecuteWizardAsync(options);
			},
			configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteWizardAsync(CliOptions options)
		{
			try
			{
				if (options.JsonOutput)
				{
					Console.Error.WriteLine("Wizard mode is not available with JSON output");
					return 1;
				}

				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				AnsiConsole.Write(new FigletText("DnsTube")
					.LeftJustified()
					.Color(Color.Blue));

				AnsiConsole.MarkupLine("[blue]Configuration Wizard[/]");
				AnsiConsole.WriteLine();

				var settings = await settingsService.GetAsync();

				// Step 1: Authentication method
				var useToken = AnsiConsole.Confirm(
					"Use API Token (recommended) instead of API Key?",
					defaultValue: true);
				settings.IsUsingToken = useToken;

				// Step 2: Email address
				settings.EmailAddress = AnsiConsole.Prompt(
					new TextPrompt<string>("Enter your Cloudflare email address:")
						.DefaultValue(settings.EmailAddress)
						.Validate(email =>
						{
							if (!email.Contains('@'))
								return ValidationResult.Error("[red]Invalid email address[/]");
							return ValidationResult.Success();
						}));

				// Step 3: API Key or Token
				settings.ApiKeyOrToken = AnsiConsole.Prompt(
					new TextPrompt<string>($"Enter your Cloudflare {(useToken ? "API Token" : "API Key")}:")
						.Secret()
						.Validate(key =>
						{
							if (string.IsNullOrWhiteSpace(key))
								return ValidationResult.Error("[red]API key/token cannot be empty[/]");
							return ValidationResult.Success();
						}));

				// Step 4: Update interval
				settings.UpdateIntervalMinutes = AnsiConsole.Prompt(
					new TextPrompt<int>("Update interval (minutes):")
						.DefaultValue(settings.UpdateIntervalMinutes > 0 ? settings.UpdateIntervalMinutes : 30)
						.Validate(minutes =>
						{
							if (minutes < 1)
								return ValidationResult.Error("[red]Interval must be at least 1 minute[/]");
							return ValidationResult.Success();
						}));

				// Step 5: Protocol support
				settings.ProtocolSupport = AnsiConsole.Prompt(
					new SelectionPrompt<IpSupport>()
						.Title("Select protocol support:")
						.AddChoices(IpSupport.IPv4, IpSupport.IPv6, IpSupport.IPv4AndIPv6));

				// Step 6: IPv4 API URL
				if (settings.ProtocolSupport != IpSupport.IPv6)
				{
					settings.IPv4_API = AnsiConsole.Prompt(
						new TextPrompt<string>("IPv4 API URL:")
							.DefaultValue(string.IsNullOrEmpty(settings.IPv4_API) ? "https://api.ipify.org/" : settings.IPv4_API)
							.Validate(url =>
							{
								if (!Uri.TryCreate(url, UriKind.Absolute, out _))
									return ValidationResult.Error("[red]Invalid URL[/]");
								return ValidationResult.Success();
							}));
				}

				// Step 7: IPv6 API URL
				if (settings.ProtocolSupport != IpSupport.IPv4)
				{
					settings.IPv6_API = AnsiConsole.Prompt(
						new TextPrompt<string>("IPv6 API URL:")
							.DefaultValue(string.IsNullOrEmpty(settings.IPv6_API) ? "https://api64.ipify.org/" : settings.IPv6_API)
							.Validate(url =>
							{
								if (!Uri.TryCreate(url, UriKind.Absolute, out _))
									return ValidationResult.Error("[red]Invalid URL[/]");
								return ValidationResult.Success();
							}));
				}
		
				// Show summary
				AnsiConsole.WriteLine();
				AnsiConsole.Write(new Rule("[yellow]Configuration Summary[/]"));
				AnsiConsole.WriteLine();

				var table = new Table();
				table.AddColumn("Setting");
				table.AddColumn("Value");
				table.AddRow("Email", settings.EmailAddress);
				table.AddRow("Authentication", settings.IsUsingToken ? "API Token" : "API Key");
				table.AddRow("API Key/Token", MaskSecret(settings.ApiKeyOrToken));
				table.AddRow("Update Interval", $"{settings.UpdateIntervalMinutes} minutes");
				table.AddRow("Protocol Support", settings.ProtocolSupport.ToString());
				if (settings.ProtocolSupport != IpSupport.IPv6)
					table.AddRow("IPv4 API", settings.IPv4_API);
				if (settings.ProtocolSupport != IpSupport.IPv4)
					table.AddRow("IPv6 API", settings.IPv6_API);
		
				AnsiConsole.Write(table);
				AnsiConsole.WriteLine();

				if (!AnsiConsole.Confirm("Save this configuration?", defaultValue: true))
				{
					output.WriteWarning("Configuration cancelled");
					return 1;
				}

				// Save configuration
				await settingsService.SaveAsync(settings);
				output.WriteSuccess("Configuration saved successfully!");

				// Ask about domain selection
				AnsiConsole.WriteLine();
				if (AnsiConsole.Confirm("Would you like to select domains now?", defaultValue: true))
				{
					output.WriteInfo("Use 'dnstube domains select' to choose domains to update");
				}

				return 0;
			}
			catch (Exception ex)
			{
				return HandleError(options, ex);
			}
		}

		#endregion

		#region config validate

		private static Command CreateValidateCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("validate", "Validate current configuration");

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

				await ExecuteValidateAsync(options);
			},
			configFileOption, configFileOnlyOption, jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteValidateAsync(CliOptions options)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				var settings = await settingsService.GetAsync();
				var validationError = settingsService.ValidateSettings(settings);

				if (options.JsonOutput)
				{
					var data = new
					{
						success = validationError == null,
						valid = validationError == null,
						error = validationError
					};
					output.WriteJson(data);
					return validationError == null ? 0 : 2;
				}

				if (validationError == null)
				{
					output.WriteSuccess("Configuration is valid");
					return 0;
				}
				else
				{
					output.WriteError($"Configuration is invalid: {validationError}");
					return 2;
				}
			}
			catch (Exception ex)
			{
				return HandleError(options, ex);
			}
		}

		#endregion

		#region config export

		private static Command CreateExportCommand(Option<string?> configFileOption, Option<string?> configFileOnlyOption,
			Option<bool> jsonOption, Option<bool> verboseOption, Option<bool> noColorOption)
		{
			var command = new Command("export", "Export configuration to JSON file");

			var outputPathArgument = new Argument<string>(
				name: "output-path",
				description: "Path to export configuration file");

			var maskSecretsOption = new Option<bool>(
				aliases: new[] { "--mask-secrets" },
				description: "Mask API key/token in exported file");

			command.AddArgument(outputPathArgument);
			command.AddOption(maskSecretsOption);

			command.SetHandler(async (string outputPath, bool maskSecrets, string? configFile,
				string? configFileOnly, bool json, bool verbose, bool noColor) =>
			{
				var options = new CliOptions
				{
					ConfigFile = configFile,
					ConfigFileOnly = configFileOnly,
					JsonOutput = json,
					Verbose = verbose,
					NoColor = noColor
				};

				await ExecuteExportAsync(options, outputPath, maskSecrets);
			},
			outputPathArgument, maskSecretsOption, configFileOption, configFileOnlyOption,
			jsonOption, verboseOption, noColorOption);

			return command;
		}

		private static async Task<int> ExecuteExportAsync(CliOptions options, string outputPath, bool maskSecrets)
		{
			try
			{
				var serviceProvider = Program.BuildServiceProvider(options);
				var output = serviceProvider.GetRequiredService<IOutputService>();
				var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

				var settings = await settingsService.GetAsync();

				// Create export object
				var exportData = new
				{
					emailAddress = settings.EmailAddress,
					isUsingToken = settings.IsUsingToken,
					apiKeyOrToken = maskSecrets ? MaskSecret(settings.ApiKeyOrToken) : settings.ApiKeyOrToken,
					updateIntervalMinutes = settings.UpdateIntervalMinutes,
					protocolSupport = settings.ProtocolSupport,
					ipv4Api = settings.IPv4_API,
					ipv6Api = settings.IPv6_API,
					selectedDomains = settings.SelectedDomains
				};

				var jsonContent = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
				{
					WriteIndented = true,
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				});

				await File.WriteAllTextAsync(outputPath, jsonContent);

				if (options.JsonOutput)
				{
					output.WriteJson(new { success = true, path = outputPath, masked = maskSecrets });
				}
				else
				{
					output.WriteSuccess($"Configuration exported to: {outputPath}");
					if (maskSecrets)
					{
						output.WriteWarning("API key/token was masked in the export");
					}
				}

				return 0;
			}
			catch (Exception ex)
			{
				return HandleError(options, ex);
			}
		}

		#endregion

		#region Helpers

		private static string MaskSecret(string secret)
		{
			if (string.IsNullOrEmpty(secret))
				return string.Empty;

			if (secret.Length <= 8)
				return new string('*', secret.Length);

			return secret.Substring(0, 4) + new string('*', secret.Length - 8) + secret.Substring(secret.Length - 4);
		}

		private static int HandleError(CliOptions options, Exception ex)
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

		#endregion
	}
}