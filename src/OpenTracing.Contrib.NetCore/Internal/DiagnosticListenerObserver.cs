using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.Configuration;

namespace OpenTracing.Contrib.NetCore.Internal
{

    internal abstract class DiagnosticListenerObserver : DiagnosticObserver, IObserver<KeyValuePair<string, object>>
    {
        private readonly GenericEventProcessor _genericEventProcessor;

        /// <summary>
        /// The name of the <see cref="DiagnosticListener"/> that should be instrumented.
        /// </summary>
        protected abstract string GetListenerName();

        protected DiagnosticListenerObserver(ILoggerFactory loggerFactory, ITracer tracer, GenericEventOptions options)
            : base(loggerFactory, tracer)
        {
            if (!options.IsIgnored(GetListenerName()))
            {
                _genericEventProcessor = new GenericEventProcessor(GetListenerName(), Tracer, Logger, options);
            }
        }

        public override IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener)
        {
            if (diagnosticListener.Name == GetListenerName())
            {
                return diagnosticListener.Subscribe(this, IsEnabled);
            }

            return null;
        }

        void IObserver<KeyValuePair<string, object>>.OnCompleted()
        {
        }

        void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
        {
        }

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                OnNext(value.Key, value.Value);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Event-Exception: {Event}", value.Key);
            }
        }

        protected virtual bool IsEnabled(string eventName)
        {
            return true;
        }

        protected abstract void OnNext(string eventName, object untypedArg);

        protected void ProcessUnhandledEvent(string eventName, object untypedArg)
        {
            _genericEventProcessor?.ProcessEvent(eventName, untypedArg);
        }

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
