using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTracing.Contrib.NetCore;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Interceptors.EntityFrameworkCore;
using OpenTracing.Contrib.NetCore.Interceptors.HttpOut;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        /// <summary>
        /// Adds instrumentation for Entity Framework Core.
        /// </summary>
        public static IOpenTracingBuilder AddEntityFrameworkCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticInterceptor, EntityFrameworkCoreInterceptor>());

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for outgoing HTTP calls.
        /// </summary>
        public static IOpenTracingBuilder AddHttpOut(this IOpenTracingBuilder builder, Action<HttpOutOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticInterceptor, HttpOutInterceptor>());

            return ConfigureHttpOut(builder, options);
        }

        /// <summary>
        /// Configuration options for the instrumentation of outgoing HTTP calls.
        /// </summary>
        /// <seealso cref="AddHttpOut(IOpenTracingBuilder, Action{HttpOutOptions})"/>
        public static IOpenTracingBuilder ConfigureHttpOut(this IOpenTracingBuilder builder, Action<HttpOutOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }
    }
}
