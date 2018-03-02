using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderExtensions
    {
        /// <summary>
        /// Adds instrumentation for ASP.NET Core, Entity Framework Core and outgoing HTTP calls.
        /// </summary>
        public static IWebHostBuilder UseOpenTracing(this IWebHostBuilder webHostBuilder, Action<IOpenTracingBuilder> configure = null)
        {
            webHostBuilder.ConfigureServices(services =>
            {
                services.AddOpenTracing(builder =>
                {
                    builder.AddAspNetCore();
                    configure?.Invoke(builder);
                });
            });

            return webHostBuilder;
        }
    }
}
