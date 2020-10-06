using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Samples.CustomersApi.DataStore;

namespace Samples.CustomersApi.Controllers
{
    [Route("customers")]
    public class CustomersController : Controller
    {
        private readonly CustomerDbContext _dbContext;
        private readonly ILogger _logger;

        public CustomersController(CustomerDbContext dbContext, ILogger<CustomersController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Json(_dbContext.Customers.ToList());
        }

        [HttpGet("{id:int}")]
        public IActionResult Index(int id)
        {
            var customer = _dbContext.Customers.FirstOrDefault(x => x.CustomerId == id);

            if (customer == null)
                return NotFound();

            // ILogger events are sent to OpenTracing as well!
            _logger.LogInformation("Returning data for customer {CustomerId}", id);

            return Json(customer);
        }
    }
}
