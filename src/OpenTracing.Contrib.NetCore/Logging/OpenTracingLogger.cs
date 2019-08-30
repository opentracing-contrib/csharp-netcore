using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Noop;
using OpenTracing.Util;

namespace OpenTracing.Contrib.NetCore.Logging
{
    internal class OpenTracingLogger : ILogger
    {
        private const string OriginalFormatPropertyName = "{OriginalFormat}";

        private readonly string _categoryName;
        private readonly IGlobalTracerAccessor _globalTracerAccessor;

        public OpenTracingLogger(IGlobalTracerAccessor globalTracerAccessor, string categoryName)
        {
            _globalTracerAccessor = globalTracerAccessor;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // Filtering should be done via the general Logging filtering feature.
            ITracer tracer = _globalTracerAccessor.GetGlobalTracer();
            return !(
                (tracer is NoopTracer) ||
                (tracer is GlobalTracer && !GlobalTracer.IsRegistered()));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                // This throws an Exception e.g. in Microsoft's DebugLogger but we don't want the app to crash if the logger has an issue.
                return;
            }

            ITracer tracer = _globalTracerAccessor.GetGlobalTracer();
            ISpan span = tracer.ActiveSpan;

            if (span == null)
            {
                // Creating a new span for a log message seems brutal so we ignore messages if we can't attach it to an active span.
                return;
            }

            if (!IsEnabled(logLevel))
            {
                return;
            }

            var fields = new Dictionary<string, object>
            {
                { "component", _categoryName },
                { "level", logLevel.ToString() }
            };

            try
            {
                if (eventId.Id != 0)
                {
                    fields["eventId"] = eventId.Id;
                }

                try
                {
                    // This throws if the argument count (message format vs. actual args) doesn't match.
                    // e.g. LogInformation("Foo {Arg1} {Arg2}", arg1);
                    // We want to preserve as much as possible from the original log message so we just continue without this information.
                    string message = formatter(state, exception);
                    fields[LogFields.Message] = message;
                }
                catch (Exception)
                {
                    /* no-op */
                }

                if (exception != null)
                {
                    fields[LogFields.ErrorKind] = exception.GetType().FullName;
                    fields[LogFields.ErrorObject] = exception;
                }

                bool eventAdded = false;

                var structure = state as IEnumerable<KeyValuePair<string, object>>;
                if (structure != null)
                {
                    try
                    {
                        // The enumerator throws if the argument count (message format vs. actual args) doesn't match.
                        // We want to preserve as much as possible from the original log message so we just ignore
                        // this error and take as many properties as possible.
                        foreach (var property in structure)
                        {
                            if (string.Equals(property.Key, OriginalFormatPropertyName, StringComparison.Ordinal)
                                 && property.Value is string messageTemplateString)
                            {
                                fields[LogFields.Event] = messageTemplateString;
                                eventAdded = true;
                            }
                            else
                            {
                                fields[property.Key] = property.Value;
                            }
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        /* no-op */
                    }
                }

                if (!eventAdded)
                {
                    fields[LogFields.Event] = "log";
                }
            }
            catch (Exception logException)
            {
                fields["opentracing.contrib.netcore.error"] = logException.ToString();
            }

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
