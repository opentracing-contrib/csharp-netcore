using System;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.NetCore.Logging
{
    /// <summary>
    /// The provider for the <see cref="OpenTracingLogger"/>.
    /// </summary>
    [ProviderAlias("OpenTracing")]
    internal class OpenTracingLoggerProvider : ILoggerProvider
    {
        private readonly ITracer _tracer;

        public OpenTracingLoggerProvider(ITracer tracer)
        {
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return new OpenTracingLogger(_tracer, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
