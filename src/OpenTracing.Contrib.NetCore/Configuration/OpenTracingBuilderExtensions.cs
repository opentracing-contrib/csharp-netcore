using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.AspNetCore;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.EntityFrameworkCore;
using OpenTracing.Contrib.NetCore.GenericListeners;
using OpenTracing.Contrib.NetCore.HttpHandler;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Contrib.NetCore.Logging;
using OpenTracing.Contrib.NetCore.MicrosoftSqlClient;
using OpenTracing.Contrib.NetCore.SystemSqlClient;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        internal static IOpenTracingBuilder AddDiagnosticSubscriber<TDiagnosticSubscriber>(this IOpenTracingBuilder builder)
            where TDiagnosticSubscriber : DiagnosticObserver
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticObserver, TDiagnosticSubscriber>());

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for ASP.NET Core.
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreDiagnosticOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<AspNetCoreDiagnostics>();
            builder.ConfigureGenericDiagnostics(genericOptions => genericOptions.IgnoredListenerNames.Add(AspNetCoreDiagnostics.DiagnosticListenerName));

            // Our default behavior for ASP.NET is that we only want spans if the request itself is traced
            builder.ConfigureEntityFrameworkCore(opt => opt.StartRootSpans = false);
            builder.ConfigureHttpHandler(opt => opt.StartRootSpans = false);
            builder.ConfigureMicrosoftSqlClient(opt => opt.StartRootSpans = false);
            builder.ConfigureSystemSqlClient(opt => opt.StartRootSpans = false);

            return ConfigureAspNetCore(builder, options);
        }

        public static IOpenTracingBuilder ConfigureAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreDiagnosticOptions> options)
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
        /// Adds instrumentation for System.Net.Http.
        /// </summary>
        public static IOpenTracingBuilder AddHttpHandler(this IOpenTracingBuilder builder, Action<HttpHandlerDiagnosticOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<HttpHandlerDiagnostics>();
            builder.ConfigureGenericDiagnostics(options => options.IgnoredListenerNames.Add(HttpHandlerDiagnostics.DiagnosticListenerName));

            return ConfigureHttpHandler(builder, options);
        }

        public static IOpenTracingBuilder ConfigureHttpHandler(this IOpenTracingBuilder builder, Action<HttpHandlerDiagnosticOptions> options)
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
        public static IOpenTracingBuilder AddEntityFrameworkCore(this IOpenTracingBuilder builder, Action<EntityFrameworkCoreDiagnosticOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<EntityFrameworkCoreDiagnostics>();
            builder.ConfigureGenericDiagnostics(genericOptions => genericOptions.IgnoredListenerNames.Add(EntityFrameworkCoreDiagnostics.DiagnosticListenerName));

            return ConfigureEntityFrameworkCore(builder, options);
        }

        /// <summary>
        /// Configuration options for the instrumentation of Entity Framework Core.
        /// </summary>
        public static IOpenTracingBuilder ConfigureEntityFrameworkCore(this IOpenTracingBuilder builder, Action<EntityFrameworkCoreDiagnosticOptions> options)
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
        /// Adds instrumentation for generic DiagnosticListeners.
        /// </summary>
        public static IOpenTracingBuilder AddGenericDiagnostics(this IOpenTracingBuilder builder, Action<GenericDiagnosticOptions> options = null)
        {
            builder.AddDiagnosticSubscriber<GenericDiagnostics>();

            return ConfigureGenericDiagnostics(builder, options);
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
        /// Disables tracing for all diagnostic listeners that don't have an explicit implementation.
        /// </summary>
        public static IOpenTracingBuilder RemoveGenericDiagnostics(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.RemoveAll<GenericDiagnostics>();

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for Microsoft.Data.SqlClient.
        /// </summary>
        public static IOpenTracingBuilder AddMicrosoftSqlClient(this IOpenTracingBuilder builder, Action<MicrosoftSqlClientDiagnosticOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<MicrosoftSqlClientDiagnostics>();
            builder.ConfigureGenericDiagnostics(genericOptions => genericOptions.IgnoredListenerNames.Add(MicrosoftSqlClientDiagnostics.DiagnosticListenerName));

            return ConfigureMicrosoftSqlClient(builder, options);
        }

        /// <summary>
        /// Configuration options for the instrumentation of Microsoft.Data.SqlClient.
        /// </summary>
        public static IOpenTracingBuilder ConfigureMicrosoftSqlClient(this IOpenTracingBuilder builder, Action<MicrosoftSqlClientDiagnosticOptions> options)
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
        /// Adds instrumentation for System.Data.SqlClient.
        /// </summary>
        public static IOpenTracingBuilder AddSystemSqlClient(this IOpenTracingBuilder builder, Action<SqlClientDiagnosticOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<SqlClientDiagnostics>();
            builder.ConfigureGenericDiagnostics(options => options.IgnoredListenerNames.Add(SqlClientDiagnostics.DiagnosticListenerName));

            return ConfigureSystemSqlClient(builder, options);
        }

        public static IOpenTracingBuilder ConfigureSystemSqlClient(this IOpenTracingBuilder builder, Action<SqlClientDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        public static IOpenTracingBuilder AddLoggerProvider(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OpenTracingLoggerProvider>());
            builder.Services.Configure<LoggerFilterOptions>(options =>
            {
                // All interesting request-specific logs are instrumented via DiagnosticSource.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.AspNetCore.Hosting", LogLevel.None);

                // The "Information"-level in ASP.NET Core is too verbose.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.AspNetCore", LogLevel.Warning);

                // EF Core is sending everything to DiagnosticSource AND ILogger so we completely disable the category.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.EntityFrameworkCore", LogLevel.None);
            });

            return builder;
        }
    }
}
