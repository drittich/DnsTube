using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using DnsTube.Core.Enums;
using DnsTube.Core.Interfaces;
using DnsTube.Core.Models;

using Microsoft.Extensions.Logging;

namespace DnsTube.Core.Services
{
	public class GitHubService : IGitHubService
	{
		private readonly ILogger<GitHubService> _logger;
		private readonly IHttpClientFactory _httpClientFactory;

		public GitHubService(ILogger<GitHubService> logger, IHttpClientFactory httpClientFactory)
		{
			_logger = logger;
			_httpClientFactory = httpClientFactory;
		}

		//get latest release version from GitHub
		public async Task<string?> GetLatestReleaseTagNameAsync()
		{
			var httpClient = _httpClientFactory.CreateClient(HttpClientName.GitHub.ToString());
			try
			{
				var response = await httpClient.GetAsync("repos/drittich/DnsTube/releases/latest");
				if (response.IsSuccessStatusCode)
				{
					var stream = await response.Content.ReadAsStreamAsync();
					var release = await JsonSerializer.DeserializeAsync<GithubRelease>(stream);

					if (release is null)
						return null;

					return release.tag_name;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning($"Error getting latest release from GitHub: {ex.Message}");
			}

			return null;
		}

	}
}
