using System;
using Microsoft.AspNetCore.Hosting;
using OpenTracing.Contrib.AspNetCore;
using OpenTracing.Contrib.AspNetCore.Interceptors.Mvc;
using OpenTracing.Contrib.AspNetCore.Interceptors.RequestIn;
using OpenTracing.Contrib.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        /// <summary>
        /// Adds instrumentation for ASP.NET Core (Incoming requests and MVC).
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.AddTransient<IStartupFilter, StartInstrumentationStartupFilter>();

            builder.Services.AddSingleton<IDiagnosticInterceptor, RequestInterceptor>();
            builder.Services.AddSingleton<IDiagnosticInterceptor, MvcInterceptor>();

            return builder;
        }
    }
}
