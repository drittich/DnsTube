using DnsTube.Core.Interfaces;

namespace DnsTube.Cli.Services
{
	/// <summary>
	/// Manages alternate configuration files
	/// </summary>
	public interface IConfigFileService
	{
		/// <summary>
		/// Load settings from a JSON config file
		/// </summary>
		Task<ISettings> LoadFromFileAsync(string path);

		/// <summary>
		/// Save settings to a JSON config file
		/// </summary>
		Task SaveToFileAsync(string path, ISettings settings);

		/// <summary>
		/// Validate that a config file exists and has valid JSON
		/// </summary>
		Task<bool> ValidateFileAsync(string path);

		/// <summary>
		/// True when using --config-file-only (standalone mode)
		/// </summary>
		bool IsStandaloneMode { get; set; }

		/// <summary>
		/// Path to config file in standalone mode
		/// </summary>
		string? ConfigFilePath { get; set; }
	}
}