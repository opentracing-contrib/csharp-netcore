using System;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.Internal;

namespace OpenTracing.Contrib.NetCore.Logging
{
    /// <summary>
    /// The provider for the <see cref="OpenTracingLogger"/>.
    /// </summary>
    [ProviderAlias("OpenTracing")]
    internal class OpenTracingLoggerProvider : ILoggerProvider
    {
        private readonly IGlobalTracerAccessor _globalTracerAccessor;

        public OpenTracingLoggerProvider(IGlobalTracerAccessor globalTracerAccessor)
        {
            _globalTracerAccessor = globalTracerAccessor ?? throw new ArgumentNullException(nameof(globalTracerAccessor));
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return new OpenTracingLogger(_globalTracerAccessor, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
