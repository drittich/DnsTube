using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DnsTube.Cli.Commands;
using DnsTube.Cli.Models;
using DnsTube.Cli.Services;
using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsTube.Cli
{
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			// Create root command
			var rootCommand = new RootCommand("DnsTube CLI - Dynamic DNS updater for Cloudflare");

			// Global options
			var configFileOption = new Option<string?>(
				aliases: new[] { "--config-file" },
				description: "Use alternate config file (reads from file, writes to database)");

			var configFileOnlyOption = new Option<string?>(
				aliases: new[] { "--config-file-only" },
				description: "Use config file exclusively (no database at all)");

			var jsonOption = new Option<bool>(
				aliases: new[] { "--json" },
				description: "Output in JSON format");

			var verboseOption = new Option<bool>(
				aliases: new[] { "--verbose", "-v" },
				description: "Enable verbose logging");

			var noColorOption = new Option<bool>(
				aliases: new[] { "--no-color" },
				description: "Disable colored output");

			// Add global options to root command
			rootCommand.AddGlobalOption(configFileOption);
			rootCommand.AddGlobalOption(configFileOnlyOption);
			rootCommand.AddGlobalOption(jsonOption);
			rootCommand.AddGlobalOption(verboseOption);
			rootCommand.AddGlobalOption(noColorOption);

			// Create commands
			var updateCommand = UpdateCommand.Create();
			var configCommand = ConfigCommand.Create();
			var domainsCommand = DomainsCommand.Create();
			var statusCommand = StatusCommand.Create();
			var listCommand = ListCommand.Create();
			var testCommand = TestCommand.Create();

			// Add commands to root
			rootCommand.AddCommand(updateCommand);
			rootCommand.AddCommand(configCommand);
			rootCommand.AddCommand(domainsCommand);
			rootCommand.AddCommand(statusCommand);
			rootCommand.AddCommand(listCommand);
			rootCommand.AddCommand(testCommand);

			// Set handler for root command to show help
			rootCommand.SetHandler(() =>
			{
				Console.WriteLine("DnsTube CLI - Use --help to see available commands");
				return Task.FromResult(0);
			});

			return await rootCommand.InvokeAsync(args);
		}

		/// <summary>
		/// Build service provider with conditional DI based on mode
		/// </summary>
		public static ServiceProvider BuildServiceProvider(CliOptions options)
		{
			var services = new ServiceCollection();

			// Register CLI options
			services.AddSingleton(options);

			// CLI-specific services (always needed)
			services.AddSingleton<IOutputService, OutputService>();
			services.AddSingleton<IConfigFileService>(sp =>
			{
				var configService = new ConfigFileService
				{
					IsStandaloneMode = options.IsStandaloneMode,
					ConfigFilePath = options.GetConfigFilePath()
				};
				return configService;
			});

			// Conditional service registration based on mode
			if (options.IsStandaloneMode)
			{
				// Standalone mode: Use file-based settings, no database
				services.AddSingleton<ISettingsService, FileSettingsService>();
				services.AddSingleton<ILogService, ConsoleLogService>();
			}
			else
			{
				// Normal mode: Use database-backed services from DnsTube.Core
				services.AddSingleton<IDbService, DbService>();
				services.AddSingleton<ISettingsService, SettingsService>();
				services.AddSingleton<ILogService, ConsoleLogService>();
			}

			// Common core services (always needed)
			services.AddSingleton<ICloudflareService, CloudflareService>();
			services.AddSingleton<IIpAddressService, IpAddressService>();
			services.AddSingleton<IGitHubService, GitHubService>();

			// Configure HTTP clients
			ConfigureHttpClients(services);

			// Logging
			services.AddLogging(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Information);
			});

			return services.BuildServiceProvider();
		}

		private static void ConfigureHttpClients(ServiceCollection services)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | 
				SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

			var clientConfigurations = new Dictionary<string, Action<HttpClient>>
			{
				[HttpClientName.Cloudflare.ToString()] = client =>
				{
					client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
					client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
				},
				[HttpClientName.GitHub.ToString()] = client =>
				{
					client.BaseAddress = new Uri("https://api.github.com/");
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
					client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
					client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
				},
				[HttpClientName.IpAddressV4.ToString()] = client =>
				{
					client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
				},
				[HttpClientName.IpAddressV6.ToString()] = client =>
				{
					client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
				}
			};

			foreach (var config in clientConfigurations)
			{
				var httpClientBuilder = services.AddHttpClient(config.Key, config.Value);
				
				// Note: Network adapter configuration would need to be dynamic
				// For CLI, we'll use default adapters unless specifically configured
				if (config.Key == HttpClientName.IpAddressV6.ToString())
				{
					ConfigureIPv6Handler(httpClientBuilder);
				}
			}
		}

		private static void ConfigureIPv6Handler(IHttpClientBuilder httpClientBuilder)
		{
			httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
			{
				var handler = new SocketsHttpHandler();
				handler.ConnectCallback = async (context, cancellationToken) =>
				{
					var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetworkV6, cancellationToken);
					var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
					socket.NoDelay = true;

					try
					{
						await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);
						return new NetworkStream(socket, ownsSocket: true);
					}
					catch
					{
						socket.Dispose();
						throw;
					}
				};

				return handler;
			});
		}
	}
}