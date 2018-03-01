using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.Core
{
    public abstract class DiagnosticInterceptor : IDiagnosticInterceptor
    {
        private IDisposable _subscription;
        private readonly bool _isTraceLoggingEnabled;

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

        public void Start()
        {
            _subscription = DiagnosticListener.AllListeners.Subscribe(listener =>
            {
                listener.SubscribeWithAdapter(this, IsEnabled);
            });
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }

        protected abstract bool IsEnabled(string listenerName);

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
                Logger.LogError(ex, "{Event} failed", callerMemberName);
            }
        }

        protected void DisposeActiveScope()
        {
            Execute(() =>
            {
                var scope = Tracer.ScopeManager.Active;
                if (scope == null)
                {
                    Logger.LogError("Scope not found");
                    return;
                }

                scope.Dispose();

                if (Tracer.ScopeManager.Active == scope)
                {
                    Logger.LogError("Disposing scope failed");
                }
            });
        }
    }
}
