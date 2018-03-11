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
                string eventName = value.Key;
                object untypedArg = value.Value;

                try
                {
                    if (_ignoredEvents != null && _ignoredEvents.Contains(eventName))
                    {
                        if (_subscriber.IsLogLevelTraceEnabled)
                        {
                            _subscriber.Logger.LogTrace("Ignoring event '{ListenerName}/{Event}'", _listenerName, eventName);
                        }

                        return;
                    }

                    Activity activity = Activity.Current;

                    if (eventName.EndsWith(".Start") && activity != null)
                    {
                        HandleActivityStart(eventName, activity, untypedArg);
                    }
                    else if (eventName.EndsWith(".Stop") && activity != null)
                    {
                        HandleActivityStop(eventName, activity);
                    }
                    else
                    {
                        HandleRegularEvent(eventName, untypedArg);
                    }
                }
                catch (Exception ex)
                {
                    _subscriber.Logger.LogWarning(ex, "Event-Exception: {ListenerName}/{Event}", _listenerName, value.Key);
                }
            }

            private void HandleActivityStart(string eventName, Activity activity, object untypedArg)
            {
                ISpanBuilder spanBuilder = _subscriber.Tracer.BuildSpan(activity.OperationName)
                    .WithTag(Tags.Component.Key, _listenerName);

                foreach (var tag in activity.Tags)
                {
                    spanBuilder.WithTag(tag.Key, tag.Value);
                }

                spanBuilder.StartActive(finishSpanOnDispose: true);
            }

            private void HandleActivityStop(string eventName, Activity activity)
            {
                IScope scope = _subscriber.Tracer.ScopeManager.Active;
                if (scope != null)
                {
                    scope.Dispose();
                }
                else
                {
                    _subscriber.Logger.LogWarning("No scope found. Event: {ListenerName}/{Event}", _listenerName, eventName);
                }
            }

            private void HandleRegularEvent(string eventName, object untypedArg)
            {
                ISpan span = _subscriber.Tracer.ActiveSpan;

                if (span != null)
                {
                    span.Log(GetLogFields(eventName, untypedArg));
                }
                else if (_subscriber.IsLogLevelTraceEnabled)
                {
                    _subscriber.Logger.LogTrace("No ActiveSpan. Event: {ListenerName}/{Event}", _listenerName, eventName);
                }
            }

            private Dictionary<string, object> GetLogFields(string eventName, object arg)
            {
                var fields = new Dictionary<string, object>
                {
                    { LogFields.Event, eventName },
                    { Tags.Component.Key, _listenerName }
                };

                // TODO improve the hell out of this... :)

                if (arg != null)
                {
                    Type argType = arg.GetType();

                    if (argType.IsPrimitive)
                    {
                        fields.Add("arg", arg);
                    }
                    else if (argType.Namespace == null)
                    {
                        // Anonymous types usually contain complex objects so their output is not really useful.
                        // Ignoring them for now.
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
