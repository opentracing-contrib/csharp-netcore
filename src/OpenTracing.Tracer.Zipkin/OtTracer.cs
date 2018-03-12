using System;
using System.Linq;
using OpenTracing.Propagation;
using zipkin4net;
using zipkin4net.Propagation;

namespace OpenTracing.Tracer.Zipkin
{
    public class OtTracer : ITracer
    {
        private readonly IInjector<ITextMap> _injector;
        private readonly IExtractor<ITextMap> _extractor;

        public IScopeManager ScopeManager { get; }

        public ISpan ActiveSpan => ScopeManager?.Active?.Span;

        public string ServiceName { get; }

        public OtTracer(string serviceName, IScopeManager scopeManager, IPropagation<string> propagation)
        {
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            ScopeManager = scopeManager ?? throw new ArgumentNullException(nameof(scopeManager));

            if (propagation == null)
                throw new ArgumentNullException(nameof(propagation));

            _injector = propagation.Injector<ITextMap>((c, key, value) => c.Set(key, value));
            _extractor = propagation.Extractor<ITextMap>((c, key) => c.Where(x => x.Key == key).Select(x => x.Value).FirstOrDefault());
        }

        public ISpanBuilder BuildSpan(string operationName)
        {
            return new OtSpanBuilder(this, operationName);
        }

        public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier)
        {
            VerifySupportedFormat(format);

            ITextMap implCarrier = GetRealCarrier(carrier);
            Trace trace = GetRealSpanContext(spanContext).Trace;

            _injector.Inject(trace.CurrentSpan, implCarrier);
        }

        public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier)
        {
            VerifySupportedFormat(format);

            ITextMap implCarrier = GetRealCarrier(carrier);
            
            ITraceContext traceContext = _extractor.Extract(implCarrier);

            if (traceContext == null)
            {
                return null;
            }

            return new OtSpanContext(Trace.CreateFromId(traceContext));
        }

        private static void VerifySupportedFormat<TCarrier>(IFormat<TCarrier> format)
        {
            if (format != BuiltinFormats.HttpHeaders && format != BuiltinFormats.TextMap)
            {
                throw new InvalidOperationException("Format " + format.ToString() + " not supported");
            }
        }

        private static ITextMap GetRealCarrier<TCarrier>(TCarrier carrier)
        {
            if (carrier == null)
            {
                throw new NullReferenceException("Carrier can't be null");
            }
            var implCarrier = carrier as ITextMap;
            if (implCarrier == null)
            {
                throw new NotSupportedException("Carriers other than ITextMap are not supported.");
            }
            return implCarrier;
        }

        private static OtSpanContext GetRealSpanContext(ISpanContext spanContext)
        {
            if (spanContext == null)
            {
                throw new NullReferenceException("SpanContext can't be null");
            }
            var impl = spanContext as OtSpanContext;
            if (impl == null)
            {
                throw new NotSupportedException("You must provide the library with SpanContext created by the itself.");
            }
            return impl;
        }
    }
}
