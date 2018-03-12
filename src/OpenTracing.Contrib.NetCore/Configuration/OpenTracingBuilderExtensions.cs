using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTracing.Contrib.NetCore.AspNetCore;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Contrib.NetCore.EntityFrameworkCore;
using OpenTracing.Contrib.NetCore.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        internal static IOpenTracingBuilder AddDiagnosticSubscriber<TDiagnosticSubscriber>(this IOpenTracingBuilder builder)
            where TDiagnosticSubscriber : DiagnosticSubscriber
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticSubscriber, TDiagnosticSubscriber>());

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for ASP.NET Core.
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<MvcDiagnostics>();
            builder.ConfigureGenericDiagnostics(MvcDiagnostics.GenericDiagnosticsExclusions);

            builder.AddDiagnosticSubscriber<RequestDiagnostics>();
            builder.ConfigureGenericDiagnostics(RequestDiagnostics.GenericDiagnosticsExclusions);

            return builder;
        }

        public static IOpenTracingBuilder ConfigureAspNetCoreRequest(this IOpenTracingBuilder builder, Action<RequestDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for the .NET framework BCL.
        /// </summary>
        public static IOpenTracingBuilder AddCoreFx(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<GenericDiagnostics>();

            builder.AddDiagnosticSubscriber<HttpHandlerDiagnostics>();
            builder.ConfigureGenericDiagnostics(HttpHandlerDiagnostics.GenericDiagnosticsExclusions);

            return builder;
        }

        public static IOpenTracingBuilder ConfigureGenericDiagnostics(this IOpenTracingBuilder builder, Action<GenericDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for Entity Framework Core.
        /// </summary>
        public static IOpenTracingBuilder AddEntityFrameworkCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<EntityFrameworkCoreDiagnostics>();
            builder.ConfigureGenericDiagnostics(EntityFrameworkCoreDiagnostics.GenericDiagnosticsExclusions);

            return builder;
        }

        /// <summary>
        /// Configuration options for the instrumentation of Entity Framework Core.
        /// </summary>
        public static IOpenTracingBuilder ConfigureEntityFrameworkCore(this IOpenTracingBuilder builder, Action<EntityFrameworkCoreOptions> options)
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
