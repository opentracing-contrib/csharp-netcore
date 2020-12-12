using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;

namespace OpenTracing.Contrib.NetCore.GenericListeners
{
    /// <summary>
    /// A <see cref="DiagnosticListener"/> subscriber that logs ALL events to <see cref="ITracer.ActiveSpan"/>.
    /// </summary>
    internal sealed class GenericDiagnostics : DiagnosticObserver
    {
        private readonly GenericDiagnosticOptions _options;

        public GenericDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<GenericDiagnosticOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public override IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener)
        {
            if (_options.IgnoredListenerNames.Contains(diagnosticListener.Name))
            {
                return null;
            }

            _options.IgnoredEvents.TryGetValue(diagnosticListener.Name, out var ignoredListenerEvents);

            return new GenericDiagnosticsSubscription(this, diagnosticListener, ignoredListenerEvents);
        }

        private class GenericDiagnosticsSubscription : IObserver<KeyValuePair<string, object>>, IDisposable
        {
            private readonly GenericDiagnostics _subscriber;
            private readonly string _listenerName;
            private readonly HashSet<string> _ignoredEvents;
            private readonly GenericEventProcessor _genericEventProcessor;

            private readonly IDisposable _subscription;


            public GenericDiagnosticsSubscription(GenericDiagnostics subscriber, DiagnosticListener diagnosticListener,
                 HashSet<string> ignoredEvents)
            {
                _subscriber = subscriber;
                _ignoredEvents = ignoredEvents;
                _listenerName = diagnosticListener.Name;

                _genericEventProcessor = new GenericEventProcessor(_listenerName, _subscriber.Tracer, subscriber.Logger);

                _subscription = diagnosticListener.Subscribe(this, IsEnabled);
            }

            public void Dispose()
            {
                _subscription.Dispose();
            }

            private bool IsEnabled(string eventName)
            {
                if (_ignoredEvents != null && _ignoredEvents.Contains(eventName))
                {
                    if (_subscriber.IsLogLevelTraceEnabled)
                    {
                        _subscriber.Logger.LogTrace("Ignoring event '{ListenerName}/{Event}'", _listenerName, eventName);
                    }

                    return false;
                }

                return true;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                string eventName = value.Key;
                object untypedArg = value.Value;

                try
                {
                    // We have to check this twice because EVERY subscriber is called
                    // if ANY subscriber returns IsEnabled=true.
                    if (!IsEnabled(eventName))
                        return;

                    _genericEventProcessor?.ProcessEvent(eventName, untypedArg);
                }
                catch (Exception ex)
                {
                    _subscriber.Logger.LogWarning(ex, "Event-Exception: {ListenerName}/{Event}", _listenerName, value.Key);
                }
            }
        }
    }
}
