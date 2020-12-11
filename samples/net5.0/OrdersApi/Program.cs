using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;

namespace Samples.OrdersApi
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
                        .UseUrls(Constants.OrdersUrl);
                })
                .ConfigureServices(services =>
                {
                    // Registers and starts Jaeger (see Shared.JaegerServiceCollectionExtensions)
                    services.AddJaeger();

                    // Enables OpenTracing instrumentation for ASP.NET Core, CoreFx, EF Core
                    services.AddOpenTracing(builder =>
                    {
                        builder.ConfigureAspNetCore(options =>
                        {
                            // We don't need any tracing data for our health endpoint.
                            options.Hosting.IgnorePatterns.Add(ctx => ctx.Request.Path == "/health");
                        });

                        builder.ConfigureEntityFrameworkCore(options =>
                        {
                            options.IgnorePatterns.Add(cmd => cmd.Command.CommandText == "SELECT 1");
                        });

                        builder.ConfigureMicrosoftSqlClient(options =>
                        {
                            options.IgnorePatterns.Add(cmd => cmd.CommandText == "SELECT 1");
                        });
                    });
                });
        }
    }
}
