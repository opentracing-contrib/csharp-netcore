using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenTracing;
using OpenTracing.Contrib.NetCore;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Util;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OpenTracing instrumentation for ASP.NET Core, CoreFx (BCL), Entity Framework Core.
        /// </summary>
        public static IServiceCollection AddOpenTracing(this IServiceCollection services, Action<IOpenTracingBuilder> builder = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var builderInstance = services.AddOpenTracingCoreServices()
                .AddAspNetCore()
                .AddCoreFx()
                .AddEntityFrameworkCore()
                .AddLoggerProvider();

            builder?.Invoke(builderInstance);

            return services;
        }

        /// <summary>
        /// Adds the core services required for OpenTracing without any actual instrumentations.
        /// </summary>
        public static IOpenTracingBuilder AddOpenTracingCoreServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<ITracer>(GlobalTracer.Instance);

            services.TryAddSingleton<DiagnosticManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, InstrumentationService>());

            var builder = new OpenTracingBuilder(services);

            return builder;
        }
    }
}
