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

                // Enables OpenTracing instrumentation for ASP.NET Core, HttpClient, EF Core
                .UseOpenTracing()

                // Register Zipkin (see Startup.Configure for how it is started)
                .ConfigureServices(services => services.AddSingleton<ZipkinManager>())

                .Build();
        }
    }
}
