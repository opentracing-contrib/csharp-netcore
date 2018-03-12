using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.NetCore.Internal
{
    internal abstract class DiagnosticSubscriber
    {
        protected ILogger Logger { get; }

        protected ITracer Tracer { get; }

        protected bool IsLogLevelTraceEnabled { get; }

        protected DiagnosticSubscriber(ILoggerFactory loggerFactory, ITracer tracer)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (tracer == null)
                throw new ArgumentNullException(nameof(tracer));

            Logger = loggerFactory.CreateLogger(GetType());
            Tracer = tracer;

            IsLogLevelTraceEnabled = Logger.IsEnabled(LogLevel.Trace);
        }

        public virtual bool IsSubscriberEnabled()
        {
            return true;
        }

        public abstract IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener);
    }
}
