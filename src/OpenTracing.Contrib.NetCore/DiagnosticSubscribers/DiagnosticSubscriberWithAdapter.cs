using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers
{
    /// <summary>
    /// Base class for instrumentation code that uses <see cref="DiagnosticListener"/> subscriptions.
    /// </summary>
    public abstract class DiagnosticSubscriberWithAdapter : DiagnosticSubscriber
    {
        /// <summary>
        /// The name of the <see cref="DiagnosticListener"/> that should be instrumented.
        /// </summary>
        protected abstract string ListenerName { get; }

        protected DiagnosticSubscriberWithAdapter(ILoggerFactory loggerFactory, ITracer tracer)
            : base(loggerFactory, tracer)
        {
        }

        public override IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener)
        {
            if (diagnosticListener.Name == ListenerName)
            {
                return diagnosticListener.SubscribeWithAdapter(this, IsEnabled);
            }

            return null;
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
                if (IsLogLevelTraceEnabled)
                    Logger.LogTrace("{Event}-Start", callerMemberName);

                action();

                if (IsLogLevelTraceEnabled)
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
