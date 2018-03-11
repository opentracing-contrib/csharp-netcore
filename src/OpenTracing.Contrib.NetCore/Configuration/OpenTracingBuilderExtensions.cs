using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.DiagnosticSubscribers;
using OpenTracing.Contrib.NetCore.DiagnosticSubscribers.AspNetCore;
using OpenTracing.Contrib.NetCore.DiagnosticSubscribers.CoreFx;
using OpenTracing.Contrib.NetCore.DiagnosticSubscribers.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        /// <summary>
        /// Adds instrumentation for ASP.NET Core.
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticSubscriber, MvcDiagnosticSubscriber>());
            builder.Services.Configure<CoreFxOptions>(x =>
            {
                x.GenericDiagnostic.IgnoreEvent(MvcDiagnosticSubscriber.DiagnosticListenerName, MvcDiagnosticSubscriber.EventBeforeAction);
                x.GenericDiagnostic.IgnoreEvent(MvcDiagnosticSubscriber.DiagnosticListenerName, MvcDiagnosticSubscriber.EventAfterAction);
                x.GenericDiagnostic.IgnoreEvent(MvcDiagnosticSubscriber.DiagnosticListenerName, MvcDiagnosticSubscriber.EventBeforeActionResult);
                x.GenericDiagnostic.IgnoreEvent(MvcDiagnosticSubscriber.DiagnosticListenerName, MvcDiagnosticSubscriber.EventAfterActionResult);
            });

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticSubscriber, RequestDiagnosticSubscriber>());
            builder.Services.Configure<CoreFxOptions>(x =>
            {
                x.GenericDiagnostic.IgnoreEvent(RequestDiagnosticSubscriber.DiagnosticListenerName, RequestDiagnosticSubscriber.EventOnActivity);
                x.GenericDiagnostic.IgnoreEvent(RequestDiagnosticSubscriber.DiagnosticListenerName, RequestDiagnosticSubscriber.EventOnActivityStart);
                x.GenericDiagnostic.IgnoreEvent(RequestDiagnosticSubscriber.DiagnosticListenerName, RequestDiagnosticSubscriber.EventOnActivityStop);
                x.GenericDiagnostic.IgnoreEvent(RequestDiagnosticSubscriber.DiagnosticListenerName, RequestDiagnosticSubscriber.EventOnUnhandledException);

                // Deprecated Hosting events
                x.GenericDiagnostic.IgnoreEvent(RequestDiagnosticSubscriber.DiagnosticListenerName, "Microsoft.AspNetCore.Hosting.BeginRequest");
                x.GenericDiagnostic.IgnoreEvent(RequestDiagnosticSubscriber.DiagnosticListenerName, "Microsoft.AspNetCore.Hosting.EndRequest");
            });

            return ConfigureAspNetCore(builder, options);
        }

        /// <summary>
        /// Configuration options for the instrumentation of ASP.NET Core.
        /// </summary>
        public static IOpenTracingBuilder ConfigureAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreOptions> options)
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
        public static IOpenTracingBuilder AddCoreFx(this IOpenTracingBuilder builder, Action<CoreFxOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticSubscriber, GenericDiagnosticSubscriber>());

            // TODO @cweiss!!
            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticSubscriber, HttpHandlerDiagnosticSubscriber>());
            //builder.Services.Configure<CoreFxOptions>(x =>
            //{
            //    x.GenericDiagnostic.IgnoredListenerNames.Add(HttpHandlerDiagnosticSubscriber.DiagnosticListenerName);
            //});

            return ConfigureCoreFx(builder, options);
        }

        /// <summary>
        /// Configuration options for the instrumentation of the .NET framework BCL.
        /// </summary>
        /// <seealso cref="AddCoreFx(IOpenTracingBuilder, Action{CoreFxOptions})"/>
        public static IOpenTracingBuilder ConfigureCoreFx(this IOpenTracingBuilder builder, Action<CoreFxOptions> options)
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
        public static IOpenTracingBuilder AddEntityFrameworkCore(this IOpenTracingBuilder builder, Action<EntityFrameworkCoreOptions> options = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticSubscriber, EFCoreDiagnosticSubscriber>());
            builder.Services.Configure<CoreFxOptions>(x =>
            {
                x.GenericDiagnostic.IgnoreEvent(EFCoreDiagnosticSubscriber.DiagnosticListenerName, EFCoreDiagnosticSubscriber.EventOnCommandExecuting);
                x.GenericDiagnostic.IgnoreEvent(EFCoreDiagnosticSubscriber.DiagnosticListenerName, EFCoreDiagnosticSubscriber.EventOnCommandExecuted);
                x.GenericDiagnostic.IgnoreEvent(EFCoreDiagnosticSubscriber.DiagnosticListenerName, EFCoreDiagnosticSubscriber.EventOnCommandError);
            });

            return ConfigureEntityFrameworkCore(builder, options);
        }

        /// <summary>
        /// Configuration options for the instrumentation of Entity Framework Core.
        /// </summary>
        /// <seealso cref="AddEntityFrameworkCore(IOpenTracingBuilder, Action{EntityFrameworkCoreOptions})"/>
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
