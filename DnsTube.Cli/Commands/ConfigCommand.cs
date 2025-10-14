using System.CommandLine;

namespace DnsTube.Cli.Commands
{
	public static class ConfigCommand
	{
		public static Command Create()
		{
			var command = new Command("config", "Manage configuration");
			
			command.SetHandler(() =>
			{
				Console.WriteLine("Config command - Implementation in progress");
				return Task.FromResult(0);
			});

			return command;
		}
	}
}