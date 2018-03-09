using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;

namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers.CoreFx
{
    public sealed class GenericDiagnosticSubscriber : DiagnosticSubscriber
    {
        private readonly GenericDiagnosticOptions _options;

        public GenericDiagnosticSubscriber(ILoggerFactory loggerFactory, ITracer tracer, IOptions<CoreFxOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value?.GenericDiagnostic ?? throw new ArgumentNullException(nameof(options));
        }

        public override IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener)
        {
            if (!_options.IgnoredListenerNames.Contains(diagnosticListener.Name))
            {
                return new GenericDiagnosticsSubscription(this, diagnosticListener);
            }

            return null;
        }

        private class GenericDiagnosticsSubscription : IObserver<KeyValuePair<string, object>>, IDisposable
        {
            private readonly GenericDiagnosticSubscriber _subscriber;
            private readonly string _listenerName;
            private readonly HashSet<string> _ignoredEvents;

            private readonly IDisposable _subscription;
            

            public GenericDiagnosticsSubscription(GenericDiagnosticSubscriber subscriber, DiagnosticListener diagnosticListener)
            {
                _subscriber = subscriber;
                _listenerName = diagnosticListener.Name;

                subscriber._options.IgnoredEvents.TryGetValue(diagnosticListener.Name, out _ignoredEvents);

                _subscription = diagnosticListener.Subscribe(this);
            }

            public void Dispose()
            {
                _subscription.Dispose();
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                if (_ignoredEvents != null && _ignoredEvents.Contains(value.Key))
                {
                    if (_subscriber.IsLogLevelTraceEnabled)
                    {
                        _subscriber.Logger.LogTrace("Ignoring event '{ListenerName}/{Event}'", _listenerName, value.Key);
                    }

                    return;
                }

                ISpan span = _subscriber.Tracer.ActiveSpan;

                if (span != null)
                {
                    span.Log(_listenerName + ": " + value.Key);
                }
                else if (_subscriber.IsLogLevelTraceEnabled)
                {
                    _subscriber.Logger.LogTrace("No ActiveSpan. Event: {ListenerName}/{Event}", _listenerName, value.Key);
                }
            }
        }
    }
}
