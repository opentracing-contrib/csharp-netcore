using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Samples.CustomersApi.DataStore;

namespace Samples.CustomersApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Adds an InMemory-Sqlite DB to show EFCore traces.
            services
                .AddEntityFrameworkSqlite()
                .AddDbContext<CustomerDbContext>(options =>
                {
                    var connectionStringBuilder = new SqliteConnectionStringBuilder
                    {
                        DataSource = ":memory:",
                        Mode = SqliteOpenMode.Memory,
                        Cache = SqliteCacheMode.Shared
                    };
                    var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);

                    options.UseSqlite(connection);
                });

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Load some dummy data into the InMemory db.
            BootstrapDataStore(app.ApplicationServices);

            app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public void BootstrapDataStore(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
                dbContext.Seed();
            }
        }
    }
}