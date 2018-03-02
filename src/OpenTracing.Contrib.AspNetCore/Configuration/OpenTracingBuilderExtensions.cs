using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTracing.Contrib.AspNetCore;
using OpenTracing.Contrib.AspNetCore.Interceptors.Mvc;
using OpenTracing.Contrib.AspNetCore.Interceptors.RequestIn;
using OpenTracing.Contrib.NetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        /// <summary>
        /// Adds instrumentation for ASP.NET Core (Incoming requests and MVC components).
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, StartInstrumentationStartupFilter>());

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticInterceptor, MvcInterceptor>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticInterceptor, RequestInterceptor>());

            return builder;
        }
    }
}
