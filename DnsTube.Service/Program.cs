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
app.MapGet("/", async context =>
{
        context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
        await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});
app.MapGet("/index", async context =>
{
        context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
        await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "index.html"));
});
app.MapGet("/settings", async context =>
{
        context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
        await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "settings.html"));
});
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
		builder.Services.AddHttpClient(config.Key, config.Value);
	}

	await Task.CompletedTask;
}