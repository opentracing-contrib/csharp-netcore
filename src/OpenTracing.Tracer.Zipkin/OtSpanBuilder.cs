using System;
using System.Collections.Generic;
using System.Globalization;
using OpenTracing.Tag;
using zipkin4net;
using zipkin4net.Annotation;

namespace OpenTracing.Tracer.Zipkin
{
    internal class OtSpanBuilder : ISpanBuilder
    {
        private readonly OtTracer _tracer;
        private readonly string _operationName;

        private DateTimeOffset _startTimestamp;
        private bool _ignoreActiveSpan;
        private Dictionary<string, string> _tags;
        private OtSpanContext _parent;

        public OtSpanBuilder(OtTracer tracer, string operationName)
        {
            _tracer = tracer;
            _operationName = operationName;
        }

        public ISpanBuilder AsChildOf(ISpanContext parent)
        {
            return AddReference(References.ChildOf, parent);
        }

        public ISpanBuilder AsChildOf(ISpan parent)
        {
            return AddReference(References.ChildOf, parent?.Context);
        }

        public ISpanBuilder AddReference(string referenceType, ISpanContext referencedContext)
        {
            if (referencedContext == null)
                return this;

            // Only one reference is supported for now.
            if (_parent != null)
                return this;

            if (referenceType == References.ChildOf || referenceType == References.FollowsFrom)
            {
                _parent = (OtSpanContext)referencedContext;
            }

            return this;
        }

        public ISpanBuilder IgnoreActiveSpan()
        {
            _ignoreActiveSpan = true;
            return this;
        }

        public ISpanBuilder WithStartTimestamp(DateTimeOffset timestamp)
        {
            _startTimestamp = timestamp;
            return this;
        }

        public ISpanBuilder WithTag(string key, string value)
        {
            if (_tags == null)
                _tags = new Dictionary<string, string>();

            _tags[key] = value;

            return this;
        }

        public ISpanBuilder WithTag(string key, bool value)
        {
            return WithTag(key, value ? "1" : "0");
        }

        public ISpanBuilder WithTag(string key, int value)
        {
            return WithTag(key, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public ISpanBuilder WithTag(string key, double value)
        {
            return WithTag(key, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        public ISpan Start()
        {
            Trace trace = CreateTrace();

            OtSpanKind spanKind = GetSpanKind();
            RecordAnnotation(trace, GetOpeningAnnotation(spanKind));

            RecordAnnotation(trace, Annotations.ServiceName(_tracer.ServiceName));
            RecordAnnotation(trace, Annotations.Rpc(_operationName));

            return new OtSpan(trace, spanKind, _tags);
        }

        public IScope StartActive(bool finishSpanOnDispose)
        {
            ISpan span = Start();

            return _tracer.ScopeManager.Activate(span, finishSpanOnDispose);
        }

        private Trace CreateTrace()
        {
            Trace parent = _parent?.Trace;

            if (parent == null && !_ignoreActiveSpan)
            {
                parent = ((OtSpan)_tracer.ActiveSpan)?.Trace;
            }

            Trace trace = parent != null ? parent.Child() : Trace.Create();

            return trace;
        }

        private OtSpanKind GetSpanKind()
        {
            if (_tags != null && _tags.TryGetValue(Tags.SpanKind.Key, out string spanKind))
            {
                return Tags.SpanKindClient == spanKind ? OtSpanKind.Client : OtSpanKind.Server;
            }
            else
            {
                return OtSpanKind.Local;
            }
        }

        private IAnnotation GetOpeningAnnotation(OtSpanKind spanKind)
        {
            switch (spanKind)
            {
                case OtSpanKind.Client:
                    return Annotations.ClientSend();
                case OtSpanKind.Server:
                    return Annotations.ServerRecv();
                case OtSpanKind.Local:
                    return Annotations.LocalOperationStart(_tracer.ServiceName);
                default:
                    throw new NotSupportedException("SpanKind: " + spanKind + " unknown.");
            }
        }

        private void RecordAnnotation(Trace trace, IAnnotation annotation)
        {
            if (_startTimestamp != default(DateTimeOffset))
            {
                trace.Record(annotation, _startTimestamp.UtcDateTime);
            }
            else
            {
                trace.Record(annotation);
            }
        }
    }
}
