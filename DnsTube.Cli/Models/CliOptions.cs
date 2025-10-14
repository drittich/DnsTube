namespace DnsTube.Cli.Models
{
	/// <summary>
	/// Global options shared across all CLI commands
	/// </summary>
	public class CliOptions
	{
		/// <summary>
		/// Path to alternate config file (reads from file, writes to database)
		/// </summary>
		public string? ConfigFile { get; set; }

		/// <summary>
		/// Path to standalone config file (no database, config file only)
		/// </summary>
		public string? ConfigFileOnly { get; set; }

		/// <summary>
		/// Enable JSON output mode
		/// </summary>
		public bool JsonOutput { get; set; }

		/// <summary>
		/// Enable verbose logging
		/// </summary>
		public bool Verbose { get; set; }

		/// <summary>
		/// Disable colored output
		/// </summary>
		public bool NoColor { get; set; }

		/// <summary>
		/// Returns true if operating in standalone mode (no database)
		/// </summary>
		public bool IsStandaloneMode => !string.IsNullOrEmpty(ConfigFileOnly);

		/// <summary>
		/// Returns the config file path if specified (standalone takes priority)
		/// </summary>
		public string? GetConfigFilePath() => ConfigFileOnly ?? ConfigFile;
	}
}