namespace DnsTube
{
	public class DnsTubeService : BackgroundService
	{
		private readonly ILogger<DnsTubeService> _logger;

		//private Settings settings;

		public DnsTubeService(ILogger<DnsTubeService> logger)
		{
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			//settings = new DnsTube.Settings();

			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				await Task.Delay(5000, stoppingToken);
			}
		}
	}
}