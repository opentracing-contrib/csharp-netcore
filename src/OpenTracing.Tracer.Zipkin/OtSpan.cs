using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenTracing.Tag;
using zipkin4net;
using zipkin4net.Annotation;

namespace OpenTracing.Tracer.Zipkin
{
    internal class OtSpan : ISpan
    {
        private readonly OtSpanKind _spanKind;
        private bool _isFinished;

        ISpanContext ISpan.Context => Context;

        public OtSpanContext Context { get; }

        public Trace Trace { get; }

        public OtSpan(Trace trace, OtSpanKind spanKind, Dictionary<string, string> tags)
        {
            _spanKind = spanKind;
            Trace = trace;
            Context = new OtSpanContext(trace);

            if (tags != null)
            {
                foreach (var entry in tags)
                {
                    SetZipkinTag(entry.Key, entry.Value);
                }
            }
        }

        public ISpan SetOperationName(string operationName)
        {
            Trace.Record(Annotations.Rpc(operationName));
            return this;
        }

        public ISpan SetTag(string key, string value)
        {
            return SetZipkinTag(key, value);
        }

        public ISpan SetTag(string key, bool value)
        {
            return SetZipkinTag(key, value ? "1" : "0");
        }

        public ISpan SetTag(string key, int value)
        {
            return SetZipkinTag(key, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public ISpan SetTag(string key, double value)
        {
            return SetZipkinTag(key, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private ISpan SetZipkinTag(string key, string value)
        {
            // Keys which result in something other than a tag.
            if (key == Tags.SamplingPriority.Key)
            {
                if (int.TryParse(value, out int i2) && i2 > 0)
                {
                    Trace.ForceSampled();
                }

                return this;
            }
            else if (key == Tags.SpanKind.Key)
            {
                // was used in SpanBuilder to define the initial annotation.
                return this;
            }

            // Some tags have special names in Zipkin
            string zipkinKey;

            if (key == Tags.Component.Key)
                zipkinKey = "lc";
            else
                zipkinKey = key;

            Trace.Record(Annotations.Tag(zipkinKey, value));
            return this;
        }

        public ISpan Log(IDictionary<string, object> fields)
        {
            Trace.Record(Annotations.Event(JoinKeyValuePairs(fields)));
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, IDictionary<string, object> fields)
        {
            Trace.Record(Annotations.Event(JoinKeyValuePairs(fields)), timestamp.UtcDateTime);
            return this;
        }

        public ISpan Log(string @event)
        {
            Trace.Record(Annotations.Event(@event));
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, string @event)
        {
            Trace.Record(Annotations.Event(@event), timestamp.UtcDateTime);
            return this;
        }

        public string GetBaggageItem(string key)
        {
            throw new NotImplementedException();
        }

        public ISpan SetBaggageItem(string key, string value)
        {
            throw new NotImplementedException();
        }

        public void Finish()
        {
            if (!_isFinished)
            {
                Trace.Record(GetClosingAnnotation(_spanKind));
                _isFinished = true;
            }
        }

        public void Finish(DateTimeOffset finishTimestamp)
        {
            if (!_isFinished)
            {
                Trace.Record(GetClosingAnnotation(_spanKind), finishTimestamp.UtcDateTime);
                _isFinished = true;
            }
        }

        private static string JoinKeyValuePairs(IDictionary<string, object> fields)
        {
            return string.Join(" ", fields.Select(entry => entry.Key + ":" + entry.Value));
        }

        private static IAnnotation GetClosingAnnotation(OtSpanKind spanKind)
        {
            switch (spanKind)
            {
                case OtSpanKind.Client:
                    return Annotations.ClientRecv();
                case OtSpanKind.Server:
                    return Annotations.ServerSend();
                case OtSpanKind.Local:
                    return Annotations.LocalOperationStop();
                default:
                    throw new NotSupportedException("SpanKind: " + spanKind + " unknown.");
            }
        }
    }
}
