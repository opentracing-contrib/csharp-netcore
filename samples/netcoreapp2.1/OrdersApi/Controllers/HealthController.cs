using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.DataStore;

namespace Samples.OrdersApi.Controllers
{
    [Route("health")]
    public class HealthController : Controller
    {
        private readonly OrdersDbContext _dbContext;

        public HealthController(OrdersDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await _dbContext.Orders.AnyAsync();

            return Ok();
        }
    }
}
