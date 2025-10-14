using System.CommandLine;

namespace DnsTube.Cli.Commands
{
	public static class ListCommand
	{
		public static Command Create()
		{
			var command = new Command("list", "List zones, domains, and adapters");
			
			command.SetHandler(() =>
			{
				Console.WriteLine("List command - Implementation in progress");
				return Task.FromResult(0);
			});

			return command;
		}
	}
}