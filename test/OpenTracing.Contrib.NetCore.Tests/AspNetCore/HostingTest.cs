using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.AspNetCore;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Mock;
using OpenTracing.Tag;
using Xunit;
using Xunit.Abstractions;

namespace OpenTracing.Contrib.NetCore.Tests.AspNetCore
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
    
    [Collection("DiagnosticSource") /* All DiagnosticSource tests must be in the same collection to ensure they are NOT run in parallel. */]
    public class HostingTest : IClassFixture<TestProgramFactory>, IDisposable
    {
        private readonly WebApplicationFactory<TestProgramFactory> _factory;
        private readonly MockTracer _tracer;
        private readonly HostingOptions _options;

        public HostingTest(TestProgramFactory factory, ITestOutputHelper output)
        {
            _tracer = new MockTracer();

            AspNetCoreDiagnosticOptions aspNetCoreOptions = new();
            _options = aspNetCoreOptions.Hosting;

            _factory = factory
                .WithWebHostBuilder(x =>
                {
                    x.ConfigureServices(services =>
                    {
                        services.AddLogging(logging =>
                        {
                            logging.AddXunit(output);
                            logging.AddFilter("OpenTracing", LogLevel.Trace);
                        });
                        services.AddOpenTracingCoreServices(builder =>
                        {
                            builder.AddAspNetCore();

                            builder.Services.AddSingleton<ITracer>(_tracer);
                            builder.Services.AddSingleton(Options.Create(aspNetCoreOptions));
                        });
                    });
                });
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
        
        private HttpClient CreateClient()
        {
            var client = _factory.CreateClient();
            return client;
        }

        [Fact]
        public async Task Request_creates_span()
        {
            var client = CreateClient();

            await client.GetAsync("/foo");
            
            var finishedSpans = _tracer.FinishedSpans();

            Assert.Single(finishedSpans);
        }

        [Fact]
        public async Task Span_has_correct_properties()
        {
            var client = CreateClient();

            await client.GetAsync("/foo");

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Empty(span.GeneratedErrors);
            
            Assert.Single(span.LogEntries);
            Assert.Equal("Microsoft.AspNetCore.Routing.EndpointMatched", span.LogEntries[0].Fields["event"]);
            
            Assert.Equal("HTTP GET", span.OperationName);
            Assert.Null(span.ParentId);
            Assert.Empty(span.References);

            Assert.Equal(5, span.Tags.Count);
            Assert.Equal(Tags.SpanKindServer, span.Tags[Tags.SpanKind.Key]);
            Assert.Equal("HttpIn", span.Tags[Tags.Component.Key]);
            Assert.Equal("GET", span.Tags[Tags.HttpMethod.Key]);
            Assert.Equal("http://localhost/foo", span.Tags[Tags.HttpUrl.Key]);
            Assert.Equal(200, span.Tags[Tags.HttpStatus.Key]);
        }

        [Fact]
        public async Task Span_has_status_404()
        {
            var client = CreateClient();

            await client.GetAsync("/not-found");
            await Task.Delay(50);

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal(404, span.Tags[Tags.HttpStatus.Key]);
        }

        [Fact]
        public async Task Extracts_trace_headers()
        {
            var client = CreateClient();

            await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/foo")
            {
                Headers =
                {
                    { "traceid", "100" },
                    { "spanid", "101" },
                }
            });

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal("100", span.Context.TraceId);
            Assert.Single(span.References);

            var reference = span.References[0];
            Assert.Equal(References.ChildOf, reference.ReferenceType);
            Assert.Equal("100", reference.Context.TraceId);
            Assert.Equal("101", reference.Context.SpanId);
        }

        [Fact]
        public async Task Does_not_Extract_trace_headers_if_disabled_in_options()
        {
            var client = CreateClient();

            await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/foo")
            {
                Headers =
                {
                    { "ignore", "1" },
                }
            });

            _options.ExtractEnabled = context => !context.Request.Headers.ContainsKey("ignore");

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Empty(span.References);
        }

        [Fact]
        public async Task Ignores_requests_with_custom_rule()
        {
            _options.IgnorePatterns.Add(context => context.Request.Headers.ContainsKey("ignore"));

            var client = CreateClient();

            await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/foo")
            {
                Headers =
                {
                    { "ignore", "1" },
                }
            });

            Assert.Empty(_tracer.FinishedSpans());
        }

        [Fact]
        public async Task Calls_Options_OperationNameResolver()
        {
            _options.OperationNameResolver = _ => "test";

            var client = CreateClient();
            await client.GetAsync("/foo");

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal("test", span.OperationName);
        }

        [Fact]
        public async Task Calls_Options_OnRequest()
        {
            bool onRequestCalled = false;
            _options.OnRequest = (_, __) => onRequestCalled = true;

            var client = CreateClient();
            await client.GetAsync("/foo");

            Assert.True(onRequestCalled);
        }

        [Fact]
        public async Task Calls_Options_OnError()
        {
            bool onErrorCalled = false;
            _options.OnError = (_, __, ___) => onErrorCalled = true;

            var client = CreateClient();
            try
            {
                await client.GetAsync("/exception");
            }
            catch (InvalidOperationException)
            {
                // The OnError handler is invoked after the request has been finished,
                // so we need to wait a little bit to make sure this test isn't failing sometimes.
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
            
            Assert.True(onErrorCalled);
        }

        [Fact]
        public async Task Creates_error_span_if_request_throws_exception()
        {
            var client = CreateClient();
            try
            {
                await client.GetAsync("/exception");
            }
            catch (InvalidOperationException)
            {
            }

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.True(span.Tags[Tags.Error.Key] as bool?);

            var logs = span.LogEntries;
            var error = logs.FirstOrDefault(x => x.Fields.TryGetValue(LogFields.Event, out object val) && val is string and "error");
            Assert.NotNull(error);
            Assert.Equal("InvalidOperationException", error.Fields[LogFields.ErrorKind]);
        }
    }
}
