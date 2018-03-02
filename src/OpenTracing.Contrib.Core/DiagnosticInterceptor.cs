using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.Core
{
    /// <summary>
    /// Base class for instrumentation code that uses <see cref="DiagnosticListener"/>.
    /// </summary>
    public abstract class DiagnosticInterceptor : IDisposable
    {
        private readonly bool _isTraceLoggingEnabled;

        private IDisposable _subscription;

        protected ILogger Logger { get; }
        protected ITracer Tracer { get; }

        protected DiagnosticInterceptor(ILoggerFactory loggerFactory, ITracer tracer)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (tracer == null)
                throw new ArgumentNullException(nameof(tracer));

            Logger = loggerFactory.CreateLogger(GetType());
            Tracer = tracer;

            _isTraceLoggingEnabled = Logger.IsEnabled(LogLevel.Trace);
        }

        /// <summary>
        /// Starts listening for <see cref="DiagnosticListener"/> events.
        /// </summary>
        public void Start()
        {
            if (_subscription != null)
            {
                // Already started.
                return;
            }

            _subscription = DiagnosticListener.AllListeners.Subscribe(listener =>
            {
                listener.SubscribeWithAdapter(this, IsEnabled);
            });
        }

        /// <summary>
        /// Stops listening for <see cref="DiagnosticListener"/> events.
        /// </summary>
        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        protected abstract bool IsEnabled(string listenerName);

        /// <summary>
        /// Executes the given <paramref name="action"/> in a fail-safe way by swallowing (and logging) any exceptions thrown.
        /// </summary>
        protected void Execute(Action action, [CallerMemberName] string callerMemberName = null)
        {
            try
            {
                if (_isTraceLoggingEnabled)
                    Logger.LogTrace("{Event}-Start", callerMemberName);

                action();

                if (_isTraceLoggingEnabled)
                    Logger.LogTrace("{Event}-End", callerMemberName);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "{Event}-Exception", callerMemberName);
            }
        }

        protected void DisposeActiveScope()
        {
            Execute(() =>
            {
                var scope = Tracer.ScopeManager.Active;
                if (scope == null)
                {
                    Logger.LogWarning("Scope not found");
                    return;
                }

                scope.Dispose();

                if (Tracer.ScopeManager.Active == scope)
                {
                    Logger.LogWarning("Disposing scope failed");
                }
            });
        }
    }
}
