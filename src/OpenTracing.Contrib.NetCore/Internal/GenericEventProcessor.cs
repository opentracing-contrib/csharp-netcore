using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.Internal
{
    internal class GenericEventProcessor
    {
        private readonly string _listenerName;
        private readonly ITracer _tracer;
        private readonly ILogger _logger;
        private readonly bool _isLogLevelTraceEnabled;

        public GenericEventProcessor(string listenerName, ITracer tracer, ILogger logger)
        {
            _listenerName = listenerName ?? throw new ArgumentNullException(nameof(listenerName));
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _isLogLevelTraceEnabled = _logger.IsEnabled(LogLevel.Trace);
        }

        public void ProcessEvent(string eventName, object untypedArg, IEnumerable<KeyValuePair<string, string>> tags = null)
        {
            Activity activity = Activity.Current;

            if (activity != null && eventName.EndsWith(".Start", StringComparison.Ordinal))
            {
                HandleActivityStart(eventName, activity, untypedArg, tags);
            }
            else if (activity != null && eventName.EndsWith(".Stop", StringComparison.Ordinal))
            {
                HandleActivityStop(eventName, activity);
            }
            else
            {
                HandleRegularEvent(eventName, untypedArg, tags);
            }
        }

        private void HandleActivityStart(string eventName, Activity activity, object untypedArg, IEnumerable<KeyValuePair<string, string>> tags)
        {
            ISpanBuilder spanBuilder = _tracer.BuildSpan(activity.OperationName)
                .WithTag(Tags.Component, _listenerName);

            foreach (var tag in activity.Tags)
            {
                spanBuilder.WithTag(tag.Key, tag.Value);
            }

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    spanBuilder.WithTag(tag.Key, tag.Value);
                }
            } 

            spanBuilder.StartActive();
        }

        private void HandleActivityStop(string eventName, Activity activity)
        {
            IScope scope = _tracer.ScopeManager.Active;
            if (scope != null)
            {
                scope.Dispose();
            }
            else
            {
                _logger.LogWarning("No scope found. Event: {ListenerName}/{Event}", _listenerName, eventName);
            }
        }

        private void HandleRegularEvent(string eventName, object untypedArg, IEnumerable<KeyValuePair<string, string>> tags)
        {
            var span = _tracer.ActiveSpan;

            if (span != null)
            {
                span.Log(GetLogFields(eventName, untypedArg, tags));
            }
            else if (_isLogLevelTraceEnabled)
            {
                _logger.LogTrace("No ActiveSpan. Event: {ListenerName}/{Event}", _listenerName, eventName);
            }
        }

        private Dictionary<string, object> GetLogFields(string eventName, object arg, IEnumerable<KeyValuePair<string, string>> tags)
        {
            var fields = new Dictionary<string, object>
                {
                    { LogFields.Event, eventName },
                    { Tags.Component.Key, _listenerName }
                };

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    fields[tag.Key] = tag.Value;
                }
            }

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

                    if (_isLogLevelTraceEnabled)
                    {
                        _logger.LogTrace("Can not extract value for argument type '{Type}'. Using ToString()", argType);
                    }
                }
            }

            return fields;
        }
    }
}
