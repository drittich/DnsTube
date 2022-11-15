namespace DnsTube.Core.Interfaces
{
	public interface ISettingsService
	{
		Task<ISettings> GetAsync(bool forceRefetch = false);
		Task SaveAsync(ISettings settings);
		Task SaveDomainsAsync(IList<SelectedDomain> domains);
		string? ValidateSettings(ISettings settings);
		Task RemoveAllSelectedDomainsAsync();
		void ClearCache();
	}
}
