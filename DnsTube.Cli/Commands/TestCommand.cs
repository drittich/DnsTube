using System.CommandLine;

namespace DnsTube.Cli.Commands
{
	public static class TestCommand
	{
		public static Command Create()
		{
			var command = new Command("test", "Test configuration and connectivity");
			
			command.SetHandler(() =>
			{
				Console.WriteLine("Test command - Implementation in progress");
				return Task.FromResult(0);
			});

			return command;
		}
	}
}