using System;
using System.Linq;
using System.Threading;
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
        static void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTracing();

            services.AddSingleton<IHostedService, Worker>();

            services.AddSingleton<ZipkinManager>();
        }

        static void StartTasks(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ZipkinManager>().Start();
        }

        /// <summary>
        /// DEMO CODE that simulates ASP.NET Core 2.1's new GenericHost.
        /// </summary>
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

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                var hostedServices = serviceProvider.GetServices<IHostedService>();

                logger.LogInformation("Starting services");
                foreach (var hostedService in hostedServices)
                {
                    await hostedService.StartAsync(CancellationToken.None);
                }

                // Wait for cancellation
                Console.ReadLine();

                logger.LogInformation("Stopping services");
                foreach (var hostedService in hostedServices.Reverse())
                {
                    await hostedService.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                }
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
