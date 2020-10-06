using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TrafficGenerator
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    // Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
                    services.AddJaeger();

                    services.AddOpenTracing();

                    services.AddHostedService<Worker>();
                });
    }
}
