using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Samples.CustomersApi.DataStore;

namespace Samples.CustomersApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Adds a Sqlite DB to show EFCore traces.
            services
                .AddDbContext<CustomerDbContext>(options =>
                {
                    options.UseSqlite("Data Source=DataStore/customers.db");
                });

            services.AddMvc();

            services.AddHealthChecks()
                .AddDbContextCheck<CustomerDbContext>();
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
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }

        private void BootstrapDataStore(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
                dbContext.Seed();
            }
        }
    }
}
