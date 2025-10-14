using System.Text.Json;
using DnsTube.Cli.Models;
using Spectre.Console;

namespace DnsTube.Cli.Services
{
	/// <summary>
	/// Handles output formatting based on mode (colored, JSON, plain text)
	/// </summary>
	public class OutputService : IOutputService
	{
		private readonly CliOptions _options;

		public OutputService(CliOptions options)
		{
			_options = options;
		}

		public void WriteSuccess(string message)
		{
			if (_options.JsonOutput)
				return;

			if (_options.NoColor || !AnsiConsole.Profile.Capabilities.Ansi)
			{
				Console.WriteLine($"✓ {message}");
			}
			else
			{
				AnsiConsole.MarkupLine($"[green]✓ {Markup.Escape(message)}[/]");
			}
		}

		public void WriteError(string message)
		{
			if (_options.JsonOutput)
				return;

			if (_options.NoColor || !AnsiConsole.Profile.Capabilities.Ansi)
			{
				Console.Error.WriteLine($"✗ {message}");
			}
			else
			{
				AnsiConsole.MarkupLine($"[red]✗ {Markup.Escape(message)}[/]");
			}
		}

		public void WriteWarning(string message)
		{
			if (_options.JsonOutput)
				return;

			if (_options.NoColor || !AnsiConsole.Profile.Capabilities.Ansi)
			{
				Console.WriteLine($"⚠ {message}");
			}
			else
			{
				AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(message)}[/]");
			}
		}

		public void WriteInfo(string message)
		{
			if (_options.JsonOutput)
				return;

			if (_options.NoColor || !AnsiConsole.Profile.Capabilities.Ansi)
			{
				Console.WriteLine(message);
			}
			else
			{
				AnsiConsole.MarkupLine($"[blue]{Markup.Escape(message)}[/]");
			}
		}

		public void WriteJson(object data)
		{
			var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			});
			Console.WriteLine(json);
		}

		public async Task<T> ShowProgressAsync<T>(string description, Func<Task<T>> operation)
		{
			if (_options.JsonOutput || _options.NoColor || !AnsiConsole.Profile.Capabilities.Ansi)
			{
				// No progress indicator in JSON mode or when colors are disabled
				return await operation();
			}

			return await AnsiConsole.Status()
				.StartAsync(description, async ctx =>
				{
					ctx.Spinner(Spinner.Known.Dots);
					ctx.SpinnerStyle(Style.Parse("green"));
					return await operation();
				});
		}

		public void WriteTable<T>(IEnumerable<T> data, params string[] columns)
		{
			if (_options.JsonOutput)
			{
				WriteJson(data);
				return;
			}

			var dataList = data.ToList();
			if (!dataList.Any())
			{
				WriteInfo("No data to display");
				return;
			}

			if (_options.NoColor || !AnsiConsole.Profile.Capabilities.Ansi)
			{
				// Simple text table
				var properties = typeof(T).GetProperties();
				var selectedProps = columns.Length > 0
					? properties.Where(p => columns.Contains(p.Name)).ToList()
					: properties.ToList();

				// Header
				Console.WriteLine(string.Join(" | ", selectedProps.Select(p => p.Name)));
				Console.WriteLine(new string('-', selectedProps.Sum(p => p.Name.Length) + (selectedProps.Count - 1) * 3));

				// Rows
				foreach (var item in dataList)
				{
					var values = selectedProps.Select(p => p.GetValue(item)?.ToString() ?? "");
					Console.WriteLine(string.Join(" | ", values));
				}
			}
			else
			{
				// Spectre.Console table
				var table = new Table();
				var properties = typeof(T).GetProperties();
				var selectedProps = columns.Length > 0
					? properties.Where(p => columns.Contains(p.Name)).ToList()
					: properties.ToList();

				foreach (var prop in selectedProps)
				{
					table.AddColumn(prop.Name);
				}

				foreach (var item in dataList)
				{
					var values = selectedProps.Select(p => Markup.Escape(p.GetValue(item)?.ToString() ?? "")).ToArray();
					table.AddRow(values);
				}

				AnsiConsole.Write(table);
			}
		}

		public void WriteLine()
		{
			if (!_options.JsonOutput)
			{
				Console.WriteLine();
			}
		}

		public void Write(string message)
		{
			if (!_options.JsonOutput)
			{
				Console.Write(message);
			}
		}
	}
}