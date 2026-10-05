using System.Net;

namespace DnsTube.Service.Tests;

public class UiRouteHeaderTests : IClassFixture<DnsTubeAppFactory>
{
	// Distinctive markers from the built index.html and settings.html pages.
	private const string IndexMarker = "id=\"ipAddressInfo\"";
	private const string SettingsMarker = "<form id=\"settings\"";

	private readonly HttpClient _client;

	public UiRouteHeaderTests(DnsTubeAppFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Theory]
	[InlineData("/", IndexMarker)]
	[InlineData("/index", IndexMarker)]
	[InlineData("/settings", SettingsMarker)]
	public async Task Extensionless_ui_route_is_served_as_utf8_html_without_caching(string path, string marker)
	{
		var response = await _client.GetAsync(path);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.NotNull(response.Content.Headers.ContentType);
		Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
		Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
		Assert.True(response.Headers.CacheControl?.NoCache, "Cache-Control: no-cache expected");
		Assert.Contains(marker, await response.Content.ReadAsStringAsync());
	}

	[Theory]
	[InlineData("/index.html", IndexMarker)]
	[InlineData("/settings.html", SettingsMarker)]
	public async Task Html_file_url_is_served_as_html_without_caching(string path, string marker)
	{
		var response = await _client.GetAsync(path);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
		Assert.True(response.Headers.CacheControl?.NoCache, "Cache-Control: no-cache expected");
		Assert.Contains(marker, await response.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task Static_icon_keeps_its_own_content_type()
	{
		var response = await _client.GetAsync("/dnstube.ico");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("image/x-icon", response.Content.Headers.ContentType?.MediaType);
	}
}
