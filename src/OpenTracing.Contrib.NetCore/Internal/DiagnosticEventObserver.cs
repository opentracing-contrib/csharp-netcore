using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.Configuration;

namespace OpenTracing.Contrib.NetCore.Internal
{
    /// <summary>
    /// Base class that allows handling events from a single <see cref="DiagnosticListener"/>.
    /// </summary>
    internal abstract class DiagnosticEventObserver
        : DiagnosticObserver, IObserver<KeyValuePair<string, object>>
    {
        private readonly DiagnosticOptions _options;
        private readonly GenericEventProcessor _genericEventProcessor;

        protected DiagnosticEventObserver(ILoggerFactory loggerFactory, ITracer tracer, DiagnosticOptions options)
            : base(loggerFactory, tracer)
        {
            _options = options;

            if (options.LogEvents)
            {
                _genericEventProcessor = new GenericEventProcessor(GetListenerName(), Tracer, Logger);
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
                if (IsEnabled(value.Key))
                {
                    HandleEvent(value.Key, value.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Event-Exception: {Event}", value.Key);
            }
        }

        /// <summary>
        /// The name of the <see cref="DiagnosticListener"/> that should be instrumented.
        /// </summary>
        protected abstract string GetListenerName();

        protected virtual bool IsSupportedEvent(string eventName) => true;

        protected abstract IEnumerable<string> HandledEventNames();

        private bool IsEnabled(string eventName)
        {
            if (!IsSupportedEvent(eventName))
                return false;

            foreach (var handledEventName in HandledEventNames())
            {
                if (handledEventName == eventName)
                    return true;
            }

            if (!_options.LogEvents || _options.IgnoredEvents.Contains(eventName))
                return false;
            
            return true;
        }

        protected abstract void HandleEvent(string eventName, object untypedArg);

        protected void HandleUnknownEvent(string eventName, object untypedArg, IEnumerable<KeyValuePair<string, string>> tags = null)
        {
            _genericEventProcessor?.ProcessEvent(eventName, untypedArg, tags);
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
