using TrafficGenerator;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
        services.AddJaeger();

        services.AddOpenTracing();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
