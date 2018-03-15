using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Mock;
using OpenTracing.Noop;

namespace OpenTracing.Contrib.NetCore.Benchmarks.AspNetCore
{
    public class RequestDiagnosticsBenchmark
    {
        private TestServer _server;
        private HttpClient _client;

        [Params(InstrumentationMode.None, InstrumentationMode.Noop, InstrumentationMode.Mock)]
        public InstrumentationMode Mode { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            ITracer tracer = null;
            bool startInstrumentationForNoopTracer = false;
            switch (Mode)
            {
                case InstrumentationMode.None:
                    tracer = NoopTracerFactory.Create();
                    break;
                case InstrumentationMode.Noop:
                    tracer = NoopTracerFactory.Create();
                    startInstrumentationForNoopTracer = true;
                    break;
                case InstrumentationMode.Mock:
                    tracer = new MockTracer();
                    break;
            }

            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices((webHostBuilderContext, services) =>
                {
                    services.AddSingleton<ITracer>(tracer);
                    services.Configure<DiagnosticManagerOptions>(o => o.StartInstrumentationForNoopTracer = startInstrumentationForNoopTracer);
                    services.AddOpenTracingCoreServices()
                        .AddAspNetCore();
                })
                .Configure(app =>
                {
                    app.Map("/foo", x =>
                    {
                        x.Run(async context => await context.Response.WriteAsync("bar"));
                    });
                }));

            _client = _server.CreateClient();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _client?.Dispose();
            _server?.Dispose();
        }

        [Benchmark]
        public async Task GetAsync()
        {
            HttpResponseMessage response = await _client.GetAsync("/foo");

            await response.Content.ReadAsStringAsync();

            // TODO: DiagnosticSource.StopActivity is called later. How to wait for it?
        }
    }
}
