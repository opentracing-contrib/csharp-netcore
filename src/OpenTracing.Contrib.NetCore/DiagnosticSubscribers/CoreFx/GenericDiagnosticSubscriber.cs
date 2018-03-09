using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers.CoreFx
{
    /// <summary>
    /// A <see cref="DiagnosticListener"/> subscriber that logs ALL events to <see cref="ITracer.ActiveSpan"/>.
    /// </summary>
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
                    span.Log(GetLogFields(value));
                }
                else if (_subscriber.IsLogLevelTraceEnabled)
                {
                    _subscriber.Logger.LogTrace("No ActiveSpan. Event: {ListenerName}/{Event}", _listenerName, value.Key);
                }
            }

            private Dictionary<string, object> GetLogFields(KeyValuePair<string, object> value)
            {
                var fields = new Dictionary<string, object>
                {
                    { LogFields.Event, value.Key },
                    { Tags.Component.Key, _listenerName }
                };

                // TODO improve the hell out of this... :)

                object arg = value.Value;

                if (arg != null)
                {
                    Type argType = arg.GetType();

                    if (argType.IsPrimitive)
                    {
                        fields.Add("arg", arg);
                    }
                    else
                    {
                        fields.Add("arg", arg.ToString());

                        if (_subscriber.IsLogLevelTraceEnabled)
                        {
                            _subscriber.Logger.LogTrace("Can not extract value for argument type '{Type}'. Using ToString()", argType);
                        }
                    }
                }

                return fields;
            }
        }
    }
}
