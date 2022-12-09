using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;

namespace Samples.FrontendWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup<Startup>()
                        .UseUrls(Constants.FrontendUrl);
                })
                .ConfigureServices(services =>
                {
                    // Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
                    services.AddJaeger();

                    // Enables OpenTracing instrumentation for ASP.NET Core, CoreFx, EF Core
                    services.AddOpenTracing();
                });
        }
    }
}
