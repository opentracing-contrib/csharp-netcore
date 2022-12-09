using System;
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
                    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Orders-net5;Trusted_Connection=True;MultipleActiveResultSets=true");
                });

            services.AddSingleton<HttpClient>();

            services.AddMvc();

            services.AddHealthChecks()
                .AddDbContextCheck<OrdersDbContext>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Load some dummy data into the db.
            BootstrapDataStore(app.ApplicationServices);

            app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHealthChecks("/health");
            });
        }

        private void BootstrapDataStore(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                dbContext.Seed();
            }
        }
    }
}
