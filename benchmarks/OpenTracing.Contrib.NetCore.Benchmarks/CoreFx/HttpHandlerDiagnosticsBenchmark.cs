using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Mock;
using OpenTracing.Noop;

namespace OpenTracing.Contrib.NetCore.Benchmarks.CoreFx
{

    public class HttpHandlerDiagnosticsBenchmark
    {
        private DiagnosticManager _diagnosticsManager;
        private HttpClient _httpClient;

        [Params(InstrumentationMode.None, InstrumentationMode.Noop, InstrumentationMode.Mock)]
        public InstrumentationMode Mode { get; set; }

        public class MockHttpMessageHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // HACK: There MUST be an awaiter otherwise exceptions are not caught by the DiagnosticsHandler.
                // https://github.com/dotnet/corefx/pull/27472
                await Task.CompletedTask;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent("Response")
                };
            }
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            ITracer tracer = null;
            bool startInstrumentForNoopTracer = false;
            switch (Mode)
            {
                case InstrumentationMode.None:
                    tracer = NoopTracerFactory.Create();
                    break;
                case InstrumentationMode.Noop:
                    tracer = NoopTracerFactory.Create();
                    startInstrumentForNoopTracer = true;
                    break;
                case InstrumentationMode.Mock:
                    tracer = new MockTracer();
                    break;
            }

            var loggerFactory = new LoggerFactory();
            var options = new HttpHandlerDiagnosticOptions();
            var interceptor = new HttpHandlerDiagnostics(loggerFactory, tracer, Options.Create(options));

            // Inner handler for mocking the result
            var httpHandler = new MockHttpMessageHandler();

            // Wrap with DiagnosticsHandler (which is internal :( )
            Type type = typeof(HttpClientHandler).Assembly.GetType("System.Net.Http.DiagnosticsHandler");
            ConstructorInfo constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
            HttpMessageHandler diagnosticsHandler = (HttpMessageHandler)constructor.Invoke(new object[] { httpHandler });

            _httpClient = new HttpClient(diagnosticsHandler);

            _diagnosticsManager = new DiagnosticManager(
                loggerFactory,
                tracer,
                new DiagnosticSubscriber[] { interceptor },
                Options.Create(new DiagnosticManagerOptions { StartInstrumentationForNoopTracer = startInstrumentForNoopTracer }));

            _diagnosticsManager.Start();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _diagnosticsManager?.Dispose();
        }

        [Benchmark]
        public Task HttpClient_GetAsync()
        {
            return _httpClient.GetAsync("http://www.example.com");
        }


    }
}
