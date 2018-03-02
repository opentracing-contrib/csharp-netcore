using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared;

namespace Samples.OrdersApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<HttpClient>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Start Zipkin (see Program.cs for how it is registered)
            app.ApplicationServices.GetRequiredService<ZipkinManager>().Start();

            app.UseDeveloperExceptionPage();

            app.UseMvc();
        }
    }
}
