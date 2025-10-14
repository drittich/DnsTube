using System.CommandLine;

namespace DnsTube.Cli.Commands
{
	public static class DomainsCommand
	{
		public static Command Create()
		{
			var command = new Command("domains", "Manage selected domains");
			
			command.SetHandler(() =>
			{
				Console.WriteLine("Domains command - Implementation in progress");
				return Task.FromResult(0);
			});

			return command;
		}
	}
}