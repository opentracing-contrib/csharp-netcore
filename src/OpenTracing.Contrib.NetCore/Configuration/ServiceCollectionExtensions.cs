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

            return services.AddOpenTracingCoreServices(otBuilder =>
            {
                otBuilder.AddLoggerProvider();
                otBuilder.AddEntityFrameworkCore();
                otBuilder.AddGenericDiagnostics();
                otBuilder.AddHttpHandler();
                otBuilder.AddMicrosoftSqlClient();
                otBuilder.AddSystemSqlClient();

                if (AssemblyExists("Microsoft.AspNetCore.Hosting"))
                {
                    otBuilder.AddAspNetCore();
                }

                builder?.Invoke(otBuilder);
            });
        }

        /// <summary>
        /// Adds the core services required for OpenTracing without any actual instrumentations.
        /// </summary>
        public static IServiceCollection AddOpenTracingCoreServices(this IServiceCollection services, Action<IOpenTracingBuilder> builder = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<ITracer>(GlobalTracer.Instance);
            services.TryAddSingleton<IGlobalTracerAccessor, GlobalTracerAccessor>();

            services.TryAddSingleton<DiagnosticManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, InstrumentationService>());

            var builderInstance = new OpenTracingBuilder(services);

            builder?.Invoke(builderInstance);

            return services;
        }

        private static bool AssemblyExists(string assemblyName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith(assemblyName))
                    return true;
            }
            return false;
        }
    }
}
