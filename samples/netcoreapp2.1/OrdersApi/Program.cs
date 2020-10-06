using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Samples.OrdersApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(Constants.OrdersUrl)
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
