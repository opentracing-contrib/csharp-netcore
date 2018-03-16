using System;
using Microsoft.Extensions.DependencyInjection;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Mock;
using OpenTracing.Noop;

namespace OpenTracing.Contrib.NetCore.Benchmarks
{
    public static class OpenTracingBuilderExtensions
    {
        public static IOpenTracingBuilder AddBenchmarkTracer(this IOpenTracingBuilder builder, InstrumentationMode mode)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            ITracer tracer = mode == InstrumentationMode.Mock
                ? new MockTracer()
                : NoopTracerFactory.Create();

            bool startInstrumentationForNoopTracer = mode == InstrumentationMode.Noop;

            builder.Services.AddSingleton<ITracer>(tracer);
            builder.Services.Configure<DiagnosticManagerOptions>(options => options.StartInstrumentationForNoopTracer = startInstrumentationForNoopTracer);

            return builder;
        }
    }
}
