using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.AspNetCore;
using OpenTracing.Mock;
using OpenTracing.Tag;
using Xunit;
using Xunit.Abstractions;

namespace OpenTracing.Contrib.NetCore.Tests.AspNetCore
{
    [Collection("DiagnosticSource") /* All DiagnosticSource tests must be in the same collection to ensure they are NOT run in parallel. */]
    public class RequestDiagnosticTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly MockTracer _tracer;

        private TestServer _server;
        private HttpClient _client;

        public RequestDiagnosticTest(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _tracer = new MockTracer();
        }

        private void StartServer(Action<RequestDiagnosticOptions> diagnosticOptions = null)
        {
            _server = new TestServer(new WebHostBuilder()
                .ConfigureServices((webHostBuilderContext, services) =>
                {
                    services.AddSingleton<ITracer>(_tracer);

                    services.AddOpenTracingCoreServices()
                        .AddAspNetCore()
                        .ConfigureAspNetCoreRequest(diagnosticOptions);
                })
                .ConfigureLogging(logging =>
                {
                    logging
                        .AddXunit(_output)
                        .AddFilter("OpenTracing", LogLevel.Trace);
                })
                .Configure(app =>
                {
                    app.Map("/foo", x =>
                    {
                        x.Run(async context => await context.Response.WriteAsync("bar"));
                    });
                    app.Map("/exception", x =>
                    {
                        x.Run(context => throw new InvalidOperationException("test exception"));
                    });
                    app.Map("/wait", x =>
                    {
                        x.Run(async context =>
                        {
                            int delay = int.Parse(context.Request.Query["delay"]);
                            await Task.Delay(TimeSpan.FromMilliseconds(delay), context.RequestAborted);
                        });
                    });
                }));

            _client = _server.CreateClient();
        }

        private Task<string> GetAsync(string requestUri, int expectedSpans = 1, Action<RequestDiagnosticOptions> options = null,
            Action<HttpClient> clientOptions = null)
        {
            return SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri), expectedSpans, options, clientOptions);
        }

        private async Task<string> SendAsync(HttpRequestMessage request, int expectedSpans = 1, Action<RequestDiagnosticOptions> options = null,
            Action<HttpClient> clientOptions = null)
        {
            StartServer(options);

            clientOptions?.Invoke(_client);

            string responseString = null;
            try
            {
                HttpResponseMessage response = await _client.SendAsync(request);

                responseString = await response.Content.ReadAsStringAsync();
            }
            finally
            {
                // Let server finish writing the DiagnosticSource events.
                int attempts = 0;
                while (_tracer.FinishedSpans().Count < expectedSpans && attempts++ < 3)
                {
                    await Task.Delay(50);
                }

                Dispose();
            }

            return responseString;
        }

        public void Dispose()
        {
            _server?.Dispose();
            _client?.Dispose();
        }

        [Fact]
        public async Task Calling_TestServer_succeeds()
        {
            string response = await GetAsync("/foo");

            Assert.Equal("bar", response);
        }

        [Fact]
        public async Task Request_creates_span()
        {
            string response = await GetAsync("/foo");

            var finishedSpans = _tracer.FinishedSpans();

            Assert.Single(finishedSpans);
        }

        [Fact]
        public async Task Span_has_correct_properties()
        {
            await GetAsync("/foo");

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Empty(span.GeneratedErrors);
            Assert.Empty(span.LogEntries);
            Assert.Equal("HTTP GET", span.OperationName);
            Assert.Equal(0, span.ParentId);
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
            await GetAsync("/not-found");

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal(404, span.Tags[Tags.HttpStatus.Key]);
        }

        [Fact]
        public async Task Extracts_trace_headers()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/foo");
            request.Headers.Add("traceid", "100");
            request.Headers.Add("spanid", "101");

            await SendAsync(request);

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal(100, span.Context.TraceId);
            Assert.Single(span.References);

            var reference = span.References[0];
            Assert.Equal(References.ChildOf, reference.ReferenceType);
            Assert.Equal(100, reference.Context.TraceId);
            Assert.Equal(101, reference.Context.SpanId);
        }

        [Fact]
        public async Task Does_not_Extract_trace_headers_if_disabled_in_options()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/foo");
            request.Headers.Add("ignore", "1");

            await SendAsync(request, options: options =>
            {
                options.ExtractEnabled = context => !context.Request.Headers.ContainsKey("ignore");
            });

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Empty(span.References);
        }

        [Fact]
        public async Task Ignores_requests_with_custom_rule()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/foo");
            request.Headers.Add("ignore", "1");

            await SendAsync(request, options: options =>
            {
                options.IgnorePatterns.Add(context => context.Request.Headers.ContainsKey("ignore"));
            });

            Assert.Empty(_tracer.FinishedSpans());
        }

        [Fact]
        public async Task Calls_Options_OperationNameResolver()
        {
            await GetAsync("/foo", options: options =>
            {
                options.OperationNameResolver = _ => "test";
            });

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal("test", span.OperationName);
        }

        [Fact]
        public async Task Calls_Options_OnRequest()
        {
            bool onRequestCalled = false;

            await GetAsync("/foo", options: options =>
            {
                options.OnRequest = (_, __) => onRequestCalled = true;
            });

            await GetAsync("/foo");

            Assert.True(onRequestCalled);
        }

        [Fact]
        public async Task Creates_error_span_if_request_throws_exception()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => GetAsync("/exception"));

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.True(span.Tags[Tags.Error.Key] as bool?);

            var logs = span.LogEntries;
            Assert.Single(logs);
            Assert.Equal("error", logs[0].Fields[LogFields.Event]);
            Assert.Equal("InvalidOperationException", logs[0].Fields[LogFields.ErrorKind]);
        }

        [Fact]
        public async Task Creates_error_span_if_client_aborts()
        {
            await Assert.ThrowsAsync<TaskCanceledException>(() => GetAsync("/wait?delay=1000", clientOptions: client =>
            {
                client.Timeout = TimeSpan.FromMilliseconds(50);
            }));

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.True(span.Tags[Tags.Error.Key] as bool?);

            var logs = span.LogEntries;
            Assert.Single(logs);
            Assert.Equal("error", logs[0].Fields[LogFields.Event]);
            Assert.Equal("TaskCanceledException", logs[0].Fields[LogFields.ErrorKind]);
        }
    }
}
