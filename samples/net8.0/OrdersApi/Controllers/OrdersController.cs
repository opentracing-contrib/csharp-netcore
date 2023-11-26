using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenTracing;
using OrdersApi.DataStore;
using Shared;

namespace OrdersApi.Controllers;

[Route("orders")]
public class OrdersController : Controller
{
    private readonly OrdersDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ITracer _tracer;

    public OrdersController(OrdersDbContext dbContext, HttpClient httpClient, ITracer tracer)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var orders = await _dbContext.Orders.ToListAsync();

        return Ok(orders.Select(x => new { x.OrderId }).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Index([FromBody] PlaceOrderCommand cmd)
    {
        var customer = await GetCustomer(cmd.CustomerId);

        var order = new Order
        {
            CustomerId = cmd.CustomerId,
            ItemNumber = cmd.ItemNumber,
            Quantity = cmd.Quantity
        };

        _dbContext.Orders.Add(order);

        await _dbContext.SaveChangesAsync();

        _tracer.ActiveSpan?.Log(new Dictionary<string, object> {
            { "event", "OrderPlaced" },
            { "orderId", order.OrderId },
            { "customer", order.CustomerId },
            { "customer_name", customer.Name },
            { "item_number", order.ItemNumber },
            { "quantity", order.Quantity }
        });

        return Ok();
    }

    private async Task<Customer> GetCustomer(int customerId)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(Constants.CustomersUrl + "customers/" + customerId)
        };

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<Customer>(body);
    }
}
