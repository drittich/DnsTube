using System.Diagnostics;
using System.Net.Http.Headers;

using DnsTube.Core.Interfaces;
using DnsTube.Core.Services;
using DnsTube.Service;

using Lib.AspNetCore.ServerSentEvents;

using Microsoft.Extensions.Hosting.WindowsServices;

var options = new WebApplicationOptions
{
	Args = args,
	ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
	ApplicationName = Process.GetCurrentProcess().ProcessName
};
var builder = WebApplication.CreateBuilder(options);

builder.Logging.AddConsole();
builder.Host.UseWindowsService();

ConfigureHttpClients(builder);

builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<ICloudflareService, CloudflareService>();
builder.Services.AddSingleton<IDbService, DbService>();
builder.Services.AddSingleton<IGitHubService, GitHubService>();
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddSingleton<IIpAddressService, IpAddressService>();

builder.Services.AddMvc();
builder.Services.AddServerSentEvents();
builder.Services.AddHostedService<WorkerService>();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints => {
	endpoints.MapControllers();
	endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
	endpoints.MapServerSentEvents("/sse");
	app.MapGet("/", () => Results.Redirect("/index.html"));
});

app.UseStaticFiles();
var defaultFilesOptions = new DefaultFilesOptions { DefaultFileNames = new List<string> { "index.html" } };
app.UseDefaultFiles(defaultFilesOptions);

await app.RunAsync();

void ConfigureHttpClients(WebApplicationBuilder builder)
{
	// use TLS 1.2
	System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;
	builder.Services.AddHttpClient(
		"Cloudflare",
		client =>
		{
			client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
		});
	builder.Services.AddHttpClient(
		"GitHub",
		client =>
		{
			client.BaseAddress = new Uri("https://api.github.com/");
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
			client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
			client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
		});
	builder.Services.AddHttpClient(
		"IpAddress",
		client =>
		{
			client.DefaultRequestHeaders.UserAgent.ParseAdd("DnsTube");
		});

}