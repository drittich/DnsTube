using System.Net;
using System.Net.Http.Headers;

using DnsTube.Core.Interfaces;

using Lib.AspNetCore.ServerSentEvents;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsTube.Service.Tests;

/// <summary>
/// The Home page refreshes its log through an EventSource on <c>/sse</c>; this guards
/// that the stream stays wired up across Lib.AspNetCore.ServerSentEvents and framework upgrades.
/// </summary>
public class LogUpdatesStreamTests : IClassFixture<DnsTubeAppFactory>
{
	private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

	private readonly DnsTubeAppFactory _factory;

	public LogUpdatesStreamTests(DnsTubeAppFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task Writing_a_log_entry_pushes_log_updated_event_to_sse_clients()
	{
		using var cts = new CancellationTokenSource(TestTimeout);
		var client = _factory.CreateClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, "/sse");
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

		using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

		var sse = _factory.Services.GetRequiredService<IServerSentEventsService>();
		while (sse.GetClients().Count == 0)
			await Task.Delay(20, cts.Token);

		await _factory.Services.GetRequiredService<ILogService>().WriteAsync("sse test entry", LogLevel.Information);

		using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cts.Token));
		string? line;
		do
		{
			line = await reader.ReadLineAsync(cts.Token);
		}
		while (line is not null && line != "event: log-updated");

		Assert.Equal("event: log-updated", line);
	}
}
