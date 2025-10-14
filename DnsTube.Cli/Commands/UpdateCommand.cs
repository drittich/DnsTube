using System.CommandLine;

namespace DnsTube.Cli.Commands
{
	public static class UpdateCommand
	{
		public static Command Create()
		{
			var command = new Command("update", "Perform DNS update");
			
			command.SetHandler(() =>
			{
				Console.WriteLine("Update command - Implementation in progress");
				return Task.FromResult(0);
			});

			return command;
		}
	}
}