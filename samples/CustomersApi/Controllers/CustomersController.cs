using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Samples.CustomersApi.DataStore;

namespace Samples.CustomersApi.Controllers
{
    [Route("customers")]
    public class CustomersController : Controller
    {
        private readonly CustomerDbContext _dbContext;

        public CustomersController(CustomerDbContext dbContext)
        {
            _dbContext = dbContext;
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

            return Json(customer);
        }
    }
}
