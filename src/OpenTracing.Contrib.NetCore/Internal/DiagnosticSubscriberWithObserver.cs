using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.NetCore.Internal
{
    internal abstract class DiagnosticSubscriberWithObserver : DiagnosticSubscriber, IObserver<KeyValuePair<string, object>>
    {
        /// <summary>
        /// The name of the <see cref="DiagnosticListener"/> that should be instrumented.
        /// </summary>
        protected abstract string ListenerName { get; }

        protected DiagnosticSubscriberWithObserver(ILoggerFactory loggerFactory, ITracer tracer)
            : base(loggerFactory, tracer)
        {
        }

        public override IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener)
        {
            if (diagnosticListener.Name == ListenerName)
            {
                return diagnosticListener.Subscribe(this);
            }

            return null;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                OnNextCore(value.Key, value.Value);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Event-Exception: {Event}", value.Key);
            }
        }

        protected abstract void OnNextCore(string eventName, object untypedArg);

        protected void DisposeActiveScope(bool isScopeRequired, Exception exception = null)
        {
            IScope scope = Tracer.ScopeManager.Active;

            if (scope != null)
            {
                if (exception != null)
                {
                    scope.Span.SetException(exception);
                }

                scope.Dispose();
            }
            else if (isScopeRequired)
            {
                Logger.LogWarning("Scope not found");
            }
        }
    }
}
