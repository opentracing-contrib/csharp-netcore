using Shared;

namespace TrafficGenerator;

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
            HttpClient customersHttpClient = new HttpClient();
            customersHttpClient.BaseAddress = new Uri(Constants.CustomersUrl);

            HttpClient ordersHttpClient = new HttpClient();
            ordersHttpClient.BaseAddress = new Uri(Constants.OrdersUrl);


            while (!stoppingToken.IsCancellationRequested)
            {
                HttpResponseMessage ordersHealthResponse = await ordersHttpClient.GetAsync("health");
                _logger.LogInformation($"Health of 'orders'-endpoint: '{ordersHealthResponse.StatusCode}'");

                HttpResponseMessage customersHealthResponse = await customersHttpClient.GetAsync("health");
                _logger.LogInformation($"Health of 'customers'-endpoint: '{customersHealthResponse.StatusCode}'");

                _logger.LogInformation("Requesting customers");

                HttpResponseMessage response = await customersHttpClient.GetAsync("customers");

                _logger.LogInformation($"Response was '{response.StatusCode}'");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            /* Application should be stopped -> no-op */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
        }
    }
}
