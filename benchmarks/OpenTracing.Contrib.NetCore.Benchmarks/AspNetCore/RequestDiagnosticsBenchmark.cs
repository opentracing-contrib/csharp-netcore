using System;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Contrib.NetCore.Benchmarks.AspNetCore
{
    public class TestProgramFactory : WebApplicationFactory<TestProgramFactory>
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            var host = WebHost.CreateDefaultBuilder()
                // https://stackoverflow.com/a/69776251/5214796
                .UseSetting("TEST_CONTENTROOT_OPENTRACING_CONTRIB_NETCORE_TESTS", "")
                .ConfigureServices(services =>
                {
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/foo", async context =>
                        {
                            await context.Response.WriteAsync("Hello");
                        });

                        endpoints.MapGet("/exception", _ =>
                        {
                            throw new InvalidOperationException("You shall not pass");
                        });
                    });
                });

            return host;
        }
    }

    public class RequestDiagnosticsBenchmark
    {
        private WebApplicationFactory<TestProgramFactory> _factory;
        private HttpClient _client;

        [Params(InstrumentationMode.None, InstrumentationMode.Noop, InstrumentationMode.Mock)]
        public InstrumentationMode Mode { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _factory = new TestProgramFactory()
                .WithWebHostBuilder(x =>
                {
                    x.ConfigureLogging(l => l.ClearProviders());

                    x.ConfigureServices(services =>
                    {
                        services.AddOpenTracingCoreServices(builder =>
                        {
                            builder.AddAspNetCore();
                            builder.AddBenchmarkTracer(Mode);
                        });
                    });
                });

            _client = _factory.CreateClient();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _factory.Dispose();
        }

        [Benchmark]
        public async Task GetAsync()
        {
            await _client.GetAsync("/foo");
        }
    }
}
