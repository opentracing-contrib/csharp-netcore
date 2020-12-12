using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using OpenTracing.Contrib.NetCore.Internal;

namespace OpenTracing.Contrib.NetCore.Benchmarks.CoreFx
{
    public class HttpHandlerDiagnosticsBenchmark
    {
        private HttpClient _httpClient;
        private ServiceProvider _serviceProvider;

        [Params(InstrumentationMode.None, InstrumentationMode.Noop, InstrumentationMode.Mock)]
        public InstrumentationMode Mode { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _httpClient = CreateHttpClient();

            _serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddOpenTracingCoreServices(builder =>
                {
                    builder.AddBenchmarkTracer(Mode);
                    builder.AddHttpHandler();
                })
                .BuildServiceProvider();

            var diagnosticsManager = _serviceProvider.GetRequiredService<DiagnosticManager>();
            diagnosticsManager.Start();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            (_serviceProvider as IDisposable).Dispose();
        }

        [Benchmark]
        public Task HttpClient_GetAsync()
        {
            return _httpClient.GetAsync("http://www.example.com");
        }

        private static HttpClient CreateHttpClient()
        {
            // Inner handler for mocking the result
            var httpHandler = new MockHttpMessageHandler();

            // Wrap with DiagnosticsHandler (which is internal :( )
            Type type = typeof(HttpClientHandler).Assembly.GetType("System.Net.Http.DiagnosticsHandler");
            ConstructorInfo constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
            HttpMessageHandler diagnosticsHandler = (HttpMessageHandler)constructor.Invoke(new object[] { httpHandler });

            return new HttpClient(diagnosticsHandler);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
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
    }
}
