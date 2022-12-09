using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Shared;

namespace FrontendWeb.Controllers;

public class HomeController : Controller
{
    private readonly HttpClient _httpClient;

    public HomeController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> PlaceOrder()
    {
        ViewBag.Customers = await GetCustomers();
        return View(new PlaceOrderCommand { ItemNumber = "ABC11", Quantity = 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(PlaceOrderCommand cmd)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await GetCustomers();
            return View(cmd);
        }

        string body = JsonConvert.SerializeObject(cmd);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(Constants.OrdersUrl + "orders"),
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
            
        await _httpClient.SendAsync(request);

        return RedirectToAction("Index");
    }

    private async Task<IEnumerable<SelectListItem>> GetCustomers()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(Constants.CustomersUrl + "customers")
        };

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<List<Customer>>(body)
            .Select(x => new SelectListItem { Value = x.CustomerId.ToString(), Text = x.Name });
    }
}
