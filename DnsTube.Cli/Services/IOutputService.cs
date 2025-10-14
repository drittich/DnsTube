namespace DnsTube.Cli.Services
{
	/// <summary>
	/// Handles output formatting based on mode (colored, JSON, plain text)
	/// </summary>
	public interface IOutputService
	{
		/// <summary>
		/// Write a success message (green)
		/// </summary>
		void WriteSuccess(string message);

		/// <summary>
		/// Write an error message (red)
		/// </summary>
		void WriteError(string message);

		/// <summary>
		/// Write a warning message (yellow)
		/// </summary>
		void WriteWarning(string message);

		/// <summary>
		/// Write an informational message
		/// </summary>
		void WriteInfo(string message);

		/// <summary>
		/// Write output in JSON format
		/// </summary>
		void WriteJson(object data);

		/// <summary>
		/// Show a progress indicator while executing an async operation
		/// </summary>
		Task<T> ShowProgressAsync<T>(string description, Func<Task<T>> operation);

		/// <summary>
		/// Write a table of data
		/// </summary>
		void WriteTable<T>(IEnumerable<T> data, params string[] columns);

		/// <summary>
		/// Write a blank line
		/// </summary>
		void WriteLine();

		/// <summary>
		/// Write a plain message without formatting
		/// </summary>
		void Write(string message);
	}
}