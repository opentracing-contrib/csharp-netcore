using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrdersApi.DataStore;

namespace Samples.OrdersApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Adds a SqlServer DB to show EFCore traces.
            services
                .AddDbContext<OrdersDbContext>(options =>
                {
                    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Orders-netcoreapp21;Trusted_Connection=True;MultipleActiveResultSets=true");
                });

            services.AddSingleton<HttpClient>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseMvc();
        }
    }
}
