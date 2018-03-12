using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared;

namespace TrafficGenerator
{
    class Program
    {
        static void StartTasks(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ZipkinManager>().Start();
        }

        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddCommandLine(args);
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOpenTracing();

                    services.AddSingleton<IHostedService, Worker>();

                    services.AddSingleton<ZipkinManager>();
                });

            await builder.RunConsoleAsync();

        }
    }
}
