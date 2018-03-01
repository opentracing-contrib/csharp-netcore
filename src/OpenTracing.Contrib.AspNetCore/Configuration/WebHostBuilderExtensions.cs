using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderExtensions
    {
        /// <summary>
        /// Adds OpenTracing instrumentation that can be sent to any compatible tracer.
        /// </summary>
        public static IWebHostBuilder UseOpenTracing(this IWebHostBuilder webHostBuilder, Action<IOpenTracingBuilder> configure = null)
        {
            webHostBuilder.ConfigureServices(services =>
            {
                var otBuilder = services.AddOpenTracing()
                    .AddAspNetCore();

                configure?.Invoke(otBuilder);
            });

            return webHostBuilder;
        }
    }
}
