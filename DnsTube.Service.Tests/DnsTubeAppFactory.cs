using DnsTube.Service;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DnsTube.Service.Tests;

/// <summary>
/// Hosts DnsTube.Service in-process with an isolated, throwaway database and no
/// background worker, so tests never touch the user's real settings or make
/// outbound calls to Cloudflare, GitHub or IP lookup services.
/// </summary>
public sealed class DnsTubeAppFactory : WebApplicationFactory<Program>
{
	private readonly string _dbFolder = Path.Combine(Path.GetTempPath(), "DnsTube.Tests", Guid.NewGuid().ToString("N"));

	public DnsTubeAppFactory()
	{
		// Program resolves the settings service before the host is built, so the
		// database location must be redirected before startup runs.
		Environment.SetEnvironmentVariable("DNSTUBE_DB_FOLDER", _dbFolder);
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.ConfigureServices(services =>
		{
			var worker = services.Single(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(WorkerService));
			services.Remove(worker);
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		Environment.SetEnvironmentVariable("DNSTUBE_DB_FOLDER", null);
		Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
		try
		{
			Directory.Delete(_dbFolder, recursive: true);
		}
		catch (IOException)
		{
			// best effort; temp folder
		}
	}
}
