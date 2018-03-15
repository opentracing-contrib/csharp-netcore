using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Mock;
using OpenTracing.Noop;
using OpenTracing.Util;
using Xunit;

namespace OpenTracing.Contrib.NetCore.Tests.Internal
{
    public class DiagnosticManagerTest
    {
        [Fact]
        public void Does_not_Start_if_Tracer_is_NoopTracer()
        {
            var loggerFactory = new NullLoggerFactory();
            var tracer = NoopTracerFactory.Create();
            var diagnosticSubscribers = new List<DiagnosticSubscriber>();
            var options = Options.Create(new DiagnosticManagerOptions());

            using (DiagnosticManager diagnosticManager = new DiagnosticManager(loggerFactory, tracer, diagnosticSubscribers, options))
            {
                Assert.False(diagnosticManager.IsRunning);

                diagnosticManager.Start();

                Assert.False(diagnosticManager.IsRunning);
            }
        }

        [Fact]
        public void Does_not_Start_if_Tracer_is_GlobalTracer_with_NoopTracer()
        {
            var loggerFactory = new NullLoggerFactory();
            var tracer = GlobalTracer.Instance;
            var diagnosticSubscribers = new List<DiagnosticSubscriber>();
            var options = Options.Create(new DiagnosticManagerOptions());

            using (DiagnosticManager diagnosticManager = new DiagnosticManager(loggerFactory, tracer, diagnosticSubscribers, options))
            {
                Assert.False(diagnosticManager.IsRunning);

                diagnosticManager.Start();

                Assert.False(diagnosticManager.IsRunning);
            }
        }

        [Fact]
        public void Start_if_valid_Tracer()
        {
            var loggerFactory = new NullLoggerFactory();
            var tracer = new MockTracer();
            var diagnosticSubscribers = new List<DiagnosticSubscriber>();
            var options = Options.Create(new DiagnosticManagerOptions());

            using (DiagnosticManager diagnosticManager = new DiagnosticManager(loggerFactory, tracer, diagnosticSubscribers, options))
            {
                Assert.False(diagnosticManager.IsRunning);

                diagnosticManager.Start();

                Assert.True(diagnosticManager.IsRunning);
            }
        }
    }
}
