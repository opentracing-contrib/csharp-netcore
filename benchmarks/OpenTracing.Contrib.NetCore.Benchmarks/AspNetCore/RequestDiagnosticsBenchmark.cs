using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

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
            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices((webHostBuilderContext, services) =>
                {
                    services.AddOpenTracingCoreServices(builder =>
                    {
                        builder.AddBenchmarkTracer(Mode);
                        builder.AddAspNetCore();
                    });
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
