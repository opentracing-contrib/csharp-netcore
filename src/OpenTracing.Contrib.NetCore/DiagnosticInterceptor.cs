using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.NetCore
{
    /// <summary>
    /// Base class for instrumentation code that uses <see cref="DiagnosticListener"/> subscriptions.
    /// </summary>
    public abstract class DiagnosticInterceptor : IDisposable
    {
        private readonly bool _isTraceLoggingEnabled;
        private object _lock = new object();

        private IDisposable _allListenersSubscription;
        private IDisposable _listenerSubscription;

        protected ILogger Logger { get; }
        protected ITracer Tracer { get; }

        /// <summary>
        /// The name of the <see cref="DiagnosticListener"/> that should be instrumented.
        /// </summary>
        protected abstract string ListenerName { get; }

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
            if (_allListenersSubscription == null)
            {
                Logger.LogTrace("Starting AllListeners subscription");

                _allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(listener =>
                {
                    if (listener.Name == ListenerName)
                    {
                        lock (_lock)
                        {
                            Logger.LogTrace("Starting named listener subscription");

                            _listenerSubscription?.Dispose();
                            _listenerSubscription = listener.SubscribeWithAdapter(this, IsEnabled);
                        }
                    }
                });
            }
            else
            {
                Logger.LogWarning("Start() was called multiple times. Call was ignored.");
            }
        }

        /// <summary>
        /// Stops listening for <see cref="DiagnosticListener"/> events.
        /// </summary>
        public void Dispose()
        {
            if (_allListenersSubscription != null)
            {
                Logger.LogTrace("Disposing AllListeners subscription");

                _allListenersSubscription.Dispose();
                _allListenersSubscription = null;

                lock (_lock)
                {
                    if (_listenerSubscription != null)
                    {
                        Logger.LogTrace("Disposing named listener subscription");

                        _listenerSubscription.Dispose();
                        _listenerSubscription = null;
                    }
                }
            }
            else
            {
                Logger.LogTrace("Dispose() called but there was no active subscription.");
            }
        }

        protected virtual bool IsEnabled(string diagnosticName, object arg1, object arg2)
        {
            return true;
        }

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
