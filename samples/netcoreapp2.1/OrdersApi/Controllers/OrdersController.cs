using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Shared;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenTracing;
using OrdersApi.DataStore;

namespace Samples.OrdersApi.Controllers
{
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

        [HttpPost]
        public async Task<IActionResult> Index([FromBody] PlaceOrderCommand cmd)
        {
            var customer = await GetCustomer(cmd.CustomerId.Value);

            var order = new Order
            {
                CustomerId = cmd.CustomerId.Value,
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
}
