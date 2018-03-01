using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared;

namespace TrafficGenerator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Hello world");

                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(Constants.CustomersUrl);

                HttpResponseMessage response = await httpClient.GetAsync("api/Customers");

                _logger.LogInformation($"Response was '{response.StatusCode}'");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
            }
        }
    }
}
