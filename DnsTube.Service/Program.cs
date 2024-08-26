using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Services;
using DnsTube.Service;

using Lib.AspNetCore.ServerSentEvents;

using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Net.Http.Headers;

var options = new WebApplicationOptions
{
	Args = args,
	ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
	ApplicationName = Process.GetCurrentProcess().ProcessName
};
var builder = WebApplication.CreateBuilder(options);

builder.Logging.AddConsole();
builder.Host.UseWindowsService();

builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IDbService, DbService>();

var settingsService = builder.Services.BuildServiceProvider().GetRequiredService<ISettingsService>();
await ConfigureHttpClientsAsync(builder, settingsService);

builder.Services.AddSingleton<ICloudflareService, CloudflareService>();
builder.Services.AddSingleton<IGitHubService, GitHubService>();
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddSingleton<IIpAddressService, IpAddressService>();

builder.Services.AddMvc();
builder.Services.AddServerSentEvents();

builder.Services.AddHostedService<WorkerService>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapServerSentEvents("/sse");
app.MapGet("/", () => Results.Redirect("/index.html"));
app.UseStaticFiles(new StaticFileOptions
{
	OnPrepareResponse = ctx =>
	{
		var requestPath = ctx.Context.Request.Path.Value;
		if (requestPath != null && requestPath.EndsWith(".html"))
		{
			ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
		}
	}
});
var defaultFilesOptions = new DefaultFilesOptions { DefaultFileNames = new List<string> { "index.html" } };
app.UseDefaultFiles(defaultFilesOptions);

await app.RunAsync();

static async Task ConfigureHttpClientsAsync(WebApplicationBuilder builder, ISettingsService settingsService)
{
	ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

	var settings = await settingsService.GetAsync();
	var selectedAdapterName = settings.NetworkAdapter;
	bool needsCustomHandler = !string.IsNullOrWhiteSpace(selectedAdapterName) && selectedAdapterName != "_DEFAULT_";

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
		var httpClientBuilder = builder.Services.AddHttpClient(config.Key, config.Value);
		if (config.Key == HttpClientName.IpAddressV6.ToString())
		{
			ConfigureIPv6Handler(httpClientBuilder, selectedAdapterName!, needsCustomHandler);
		}
		else if (needsCustomHandler)
		{
			ConfigureIPv4Handler(httpClientBuilder, selectedAdapterName!);
		}
	}
}

static void ConfigureIPv4Handler(IHttpClientBuilder httpClientBuilder, string selectedAdapterName)
{
	httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
	{
		var handler = new SocketsHttpHandler();
		handler.ConnectCallback = async (context, cancellationToken) =>
		{
			var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			var ipAddress = GetNetworkAdapterIPAddress(selectedAdapterName, AddressFamily.InterNetwork);
			var localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 0);
			socket.Bind(localEndPoint);
			await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
			return new NetworkStream(socket, ownsSocket: true);
		};

		return handler;
	});
}


static void ConfigureIPv6Handler(IHttpClientBuilder httpClientBuilder, string selectedAdapterName, bool needsCustomHandler)
{
	httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
	{
		var handler = new SocketsHttpHandler();
		handler.ConnectCallback = async (context, cancellationToken) =>
		{
			var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetworkV6, cancellationToken);
			var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			socket.NoDelay = true;
			if (needsCustomHandler)
			{
				var ipAddress = GetNetworkAdapterIPAddress(selectedAdapterName, AddressFamily.InterNetworkV6);
				var localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 0);
				socket.Bind(localEndPoint);
			}

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


static string GetNetworkAdapterIPAddress(string? adapterName, AddressFamily addressFamily)
{
	ArgumentNullException.ThrowIfNull(adapterName);

	var adapter = NetworkInterface.GetAllNetworkInterfaces()
		.Where(a => a.Name == adapterName)
		.FirstOrDefault();

	if (adapter is null)
	{
		throw new Exception($"Can't find adapter [{adapterName}], check your settings");
	}

	var properties = adapter.GetIPProperties();
	var ipAddress = properties.UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == addressFamily)?.Address;

	if (ipAddress is null)
	{
		throw new Exception($"Error getting IP address for adapter [{adapterName}]");
	}

	return ipAddress.ToString();
}