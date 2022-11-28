using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared;

namespace Samples.CustomersApi
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
                        .UseUrls(Constants.CustomersUrl);
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
                            // This is an example for how certain EF Core commands can be ignored.
                            // As en example, we're ignoring the "PRAGMA foreign_keys=ON;" commands that are executed by Sqlite.
                            // Remove this code to see those statements.
                            options.IgnorePatterns.Add(cmd => cmd.Command.CommandText.StartsWith("PRAGMA"));
                        });
                    });

                });
        }
    }
}
