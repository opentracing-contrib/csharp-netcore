using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.NetCore.Internal.Logging
{
    internal class OpenTracingLogger : ILogger
    {
        private const string OriginalFormatPropertyName = "{OriginalFormat}";

        private readonly ITracer _tracer;
        private readonly string _categoryName;

        public OpenTracingLogger(ITracer tracer, string categoryName)
        {
            _tracer = tracer;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // Filtering should be done via the general Logging filtering feature.

            return !_tracer.IsNoopTracer();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                // This throws an Exception e.g. in Microsoft's DebugLogger but I don't want the app to crash if the logger has an issue.
                return;
            }

            ISpan span = _tracer.ActiveSpan;

            if (span == null)
            {
                // Creating a new span for a log message seems brutal so we ignore messages if we can't attach it to an active span.
                return;
            }

            string message = formatter(state, exception);

            var fields = new Dictionary<string, object>
            {
                { "component", _categoryName },
                { "level", logLevel.ToString() },
                { LogFields.Message, message }
            };

            if (eventId.Id != 0)
            {
                fields.Add("eventId", eventId.Id);
            }

            if (exception != null)
            {
                fields.Add(LogFields.ErrorKind, exception.GetType().Name);
                fields.Add(LogFields.ErrorObject, exception);
            }

            string eventName = null;

            var structure = state as IEnumerable<KeyValuePair<string, object>>;
            if (structure != null)
            {
                foreach (var property in structure)
                {
                    if (property.Key == OriginalFormatPropertyName && property.Value is string messageTemplateString)
                    {
                        eventName = messageTemplateString;
                    }
                    else
                    {
                        fields.Add(property.Key, property.Value);
                    }
                }
            }

            if (eventName == null)
            {
                eventName = "log";
            }

            fields.Add(LogFields.Event, eventName);

            span.Log(fields);
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}
