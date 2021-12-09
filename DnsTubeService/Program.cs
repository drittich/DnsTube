using DnsTube;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "DnsTube Service";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<DnsTubeService>();
    })
    .Build();

await host.RunAsync();
