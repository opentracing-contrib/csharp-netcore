using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing.Propagation;
using Xunit;

namespace OpenTracing.Contrib.Netcore.Tests.Logging
{
    public class LoggingDependencyInjectionTest
    {
        [Fact]
        public void Resolving_tracer_that_needs_ILoggerFactory_succeeds()
        {
            // https://github.com/opentracing-contrib/csharp-netcore/issues/14

            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddOpenTracingCoreServices(ot =>
                {
                    ot.AddLoggerProvider();
                    ot.Services.AddSingleton<ITracer, TracerWithLoggerFactory>();
                })
                .BuildServiceProvider();

            var tracer = serviceProvider.GetRequiredService<ITracer>();
            Assert.IsType<TracerWithLoggerFactory>(tracer);
        }

        private class TracerWithLoggerFactory : ITracer
        {
            public TracerWithLoggerFactory(ILoggerFactory loggerFactory)
            {
            }

            public IScopeManager ScopeManager => throw new NotSupportedException();

            public ISpan ActiveSpan => throw new NotSupportedException();

            public ISpanBuilder BuildSpan(string operationName)
            {
                throw new NotSupportedException();
            }

            public ISpanContext Extract<TCarrier>(IFormat<TCarrier> format, TCarrier carrier)
            {
                throw new NotSupportedException();
            }

            public void Inject<TCarrier>(ISpanContext spanContext, IFormat<TCarrier> format, TCarrier carrier)
            {
                throw new NotSupportedException();
            }
        }
    }
}
