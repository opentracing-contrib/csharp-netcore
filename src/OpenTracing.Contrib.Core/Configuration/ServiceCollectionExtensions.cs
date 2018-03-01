using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTracing;
using OpenTracing.Contrib.Core;
using OpenTracing.Contrib.Core.Configuration;
using OpenTracing.Util;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IOpenTracingBuilder AddOpenTracing(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var builder = services.AddOpenTracingCore()
                .AddEntityFrameworkCore()
                .AddHttpClient();

            return builder;
        }

        public static IOpenTracingBuilder AddOpenTracingCore(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<ITracer>(GlobalTracer.Instance);

            services.TryAddSingleton<IInstrumentor, Instrumentor>();

            var builder = new OpenTracingBuilder(services);

            return builder;
        }
    }
}
