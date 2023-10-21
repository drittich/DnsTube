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

	var selectedAdapterName = (await settingsService.GetAsync()).NetworkAdapter;
	bool needsCustomHandler = !string.IsNullOrWhiteSpace(selectedAdapterName) && selectedAdapterName != "_DEFAULT_";

	IHttpClientBuilder httpClientBuilder;

	httpClientBuilder = builder.Services.AddHttpClient(
		HttpClientName.Cloudflare.ToString(),
		client =>
		{
			client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
		});
	if (needsCustomHandler)
	{
		ConfigureHandler(httpClientBuilder, selectedAdapterName!);
	}

	httpClientBuilder = builder.Services.AddHttpClient(
		HttpClientName.GitHub.ToString(),
		client =>
		{
			client.BaseAddress = new Uri("https://api.github.com/");
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
			client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
			client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
		});
	if (needsCustomHandler)
	{
		ConfigureHandler(httpClientBuilder, selectedAdapterName!);
	}

	httpClientBuilder = builder.Services.AddHttpClient(
		HttpClientName.IpAddress.ToString(),
		(client) =>
		{
			client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
		});
	if (needsCustomHandler)
	{
		ConfigureHandler(httpClientBuilder, selectedAdapterName!);
	}
}

static void ConfigureHandler(IHttpClientBuilder httpClientBuilder, string selectedAdapterName)
{
	httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
	{
		var handler = new SocketsHttpHandler();
		handler.ConnectCallback = async (context, cancellationToken) =>
		{
			var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			var ipAddress = GetNetworkAdapterIPAddress(selectedAdapterName);
			var localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 0);
			socket.Bind(localEndPoint);
			await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
			return new NetworkStream(socket, ownsSocket: true);
		};

		return handler;
	});
}

static string GetNetworkAdapterIPAddress(string? adapterName)
{
	if (adapterName is null)
	{
		throw new ArgumentNullException(nameof(adapterName));
	}

	var adapter = NetworkInterface.GetAllNetworkInterfaces()
		.Where(a => a.Name == adapterName)
		.FirstOrDefault();

	if (adapter is null)
	{
		throw new Exception($"Can't find adapter [{adapterName}]");
	}

	var properties = adapter.GetIPProperties();
	var ipAddress = properties.UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;

	if (ipAddress is null)
	{
		throw new Exception($"Error getting IP address for adapter [{adapterName}]");
	}

	return ipAddress.ToString();
}