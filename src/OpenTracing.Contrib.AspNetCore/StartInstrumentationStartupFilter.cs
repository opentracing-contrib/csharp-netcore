using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using OpenTracing.Contrib.Core;

namespace OpenTracing.Contrib.AspNetCore
{
    /// <summary>
    /// Automatically starts the instrumentation on application startup.
    /// </summary>
    internal class StartInstrumentationStartupFilter : IStartupFilter
    {
        private readonly IOpenTracingInstrumentor _instrumentor;

        public StartInstrumentationStartupFilter(IOpenTracingInstrumentor instrumentor)
        {
            _instrumentor = instrumentor ?? throw new ArgumentNullException(nameof(instrumentor));
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            _instrumentor.Start();

            return next;
        }
    }
}
