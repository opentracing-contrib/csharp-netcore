using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore;
using Shared;

namespace TrafficGenerator
{
    class Program
    {
        static void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTracing();
            services.AddSingleton<ZipkinManager>();
        }

        static void StartTasks(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ZipkinManager>().Start();
            serviceProvider.GetRequiredService<IOpenTracingInstrumentor>().Start();
        }

        static async Task Main(string[] args)
        {
            // Configuration

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .Build();

            // Dependency Injection

            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddOptions()
                .AddLogging(logging =>
                {
                    logging.AddConfiguration(configuration.GetSection("Logging"));
                    logging.AddConsole();
                });

            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();

            StartTasks(serviceProvider);

            // Start Worker

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting worker");

            try
            {
                // Start Worker
                var worker = (Worker)ActivatorUtilities.CreateInstance(serviceProvider, typeof(Worker));
                await worker.StartAsync(CancellationToken.None);

                // Wait for cancellation
                Console.ReadLine();

                logger.LogInformation("Closing application");
                await worker.StopAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception");
                Console.ReadLine();
            }
            finally
            {
                (serviceProvider as IDisposable)?.Dispose();
            }

        }


    }
}
