using System.Text.Json;
using System.Text.RegularExpressions;

namespace DnsTube.Core
{
	public class Utility
	{
		public static string GetSettingsFilePath()
		{
#if PORTABLE
				// save config in application directory
				var appDirectory = Directory.GetCurrentDirectory();
#elif SERVICE
				// save config in application directory
				var appDirectory = Directory.GetCurrentDirectory();
#else
			// save config under user profile
			string localApplicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string appDirectory = Path.Combine(localApplicationDataDirectory, "DnsTube");
			Directory.CreateDirectory(appDirectory);
#endif
			return Path.Combine(appDirectory, "config.json");
		}

		public static string GetDateString()
		{
			return DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");
		}

		public static GithubRelease GetLatestRelease()
		{
			var url = "https://api.github.com/repos/drittich/DnsTube/releases/latest";

			GithubRelease release = null;
			using (var client = new HttpClient())
			{
				client.Timeout = TimeSpan.FromSeconds(15);

				try
				{
					client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
					client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
					var response = client.GetStringAsync(url).Result;
					release = JsonSerializer.Deserialize<GithubRelease>(response);
				}
				catch { }
			}
			return release;
		}
	}
}
