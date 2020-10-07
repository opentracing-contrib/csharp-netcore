using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samples.CustomersApi.DataStore;

namespace CustomersApi.Controllers
{
    [Route("health")]
    public class HealthController : Controller
    {
        private readonly CustomerDbContext _dbContext;

        public HealthController(CustomerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await _dbContext.Customers.FirstOrDefaultAsync();

            return Ok();
        }
    }
}
