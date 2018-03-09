using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Samples.FrontendWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(Constants.FrontendUrl)
                .ConfigureServices(services =>
                {
                    // Enables OpenTracing instrumentation for ASP.NET Core, HttpClient, EF Core
                    services.AddOpenTracing();

                    // Register Zipkin (see Startup.Configure for how it is started)
                    services.AddSingleton<ZipkinManager>();
                })
                .Build();
        }
    }
}
