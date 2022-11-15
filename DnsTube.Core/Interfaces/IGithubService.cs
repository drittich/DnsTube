namespace DnsTube.Core.Interfaces
{
	public interface IGitHubService
	{
		Task<string?> GetLatestReleaseTagNameAsync();
	}
}
