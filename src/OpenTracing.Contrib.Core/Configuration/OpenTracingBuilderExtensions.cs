using System;
using OpenTracing.Contrib.Core;
using OpenTracing.Contrib.Core.Configuration;
using OpenTracing.Contrib.Core.Interceptors.EntityFrameworkCore;
using OpenTracing.Contrib.Core.Interceptors.HttpOut;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        /// <summary>
        /// Traces Entity Framework Core commands.
        /// </summary>
        public static IOpenTracingBuilder AddEntityFrameworkCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddSingleton<IDiagnosticInterceptor, EntityFrameworkCoreInterceptor>();

            return builder;
        }

        /// <summary>
        /// Traces <see cref="System.Net.Http.HttpClient"/> calls.
        /// </summary>
        public static IOpenTracingBuilder AddHttpClient(this IOpenTracingBuilder builder, Action<HttpOutOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddSingleton<IDiagnosticInterceptor, HttpOutInterceptor>();

            return ConfigureHttpClient(builder, options);
        }

        /// <summary>
        /// Configuration for the instrumentation of <see cref="System.Net.Http.HttpClient"/> calls.
        /// </summary>
        /// <seealso cref="AddHttpClient(IOpenTracingBuilder, Action{HttpOutOptions})"/>
        public static IOpenTracingBuilder ConfigureHttpClient(this IOpenTracingBuilder builder, Action<HttpOutOptions> options)
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
