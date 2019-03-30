using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Mock;
using OpenTracing.Tag;
using Xunit;
using Xunit.Abstractions;

namespace OpenTracing.Contrib.NetCore.Tests.CoreFx
{
    [Collection("DiagnosticSource") /* All DiagnosticSource tests must be in the same collection to ensure they are NOT run in parallel. */]
    public class HttpHandlerDiagnosticTest : IDisposable
    {
        private readonly MockTracer _tracer;
        private readonly HttpHandlerDiagnosticOptions _options;
        private readonly DiagnosticManager _diagnosticsManager;
        private readonly MockHttpMessageHandler _httpHandler;
        private readonly HttpClient _httpClient;

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, HttpResponseMessage> OnSend = request =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent("Response")
                };
            };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // HACK: There MUST be an awaiter otherwise exceptions are not caught by the DiagnosticsHandler.
                // https://github.com/dotnet/corefx/pull/27472
                await Task.CompletedTask;

                return OnSend(request);
            }
        }

        public HttpHandlerDiagnosticTest(ITestOutputHelper output)
        {
            _tracer = new MockTracer();
            _options = new HttpHandlerDiagnosticOptions();

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging(logging =>
                {
                    logging.AddXunit(output);
                })
                .AddOpenTracingCoreServices(builder =>
                {
                    builder.AddCoreFx();
                    builder.Services.AddSingleton<ITracer>(_tracer);
                    builder.Services.AddSingleton(Options.Create(_options));
                })
                .BuildServiceProvider();

            _diagnosticsManager = serviceProvider.GetRequiredService<DiagnosticManager>();
            _diagnosticsManager.Start();

            // Inner handler for mocking the result
            _httpHandler = new MockHttpMessageHandler();

            // Wrap with DiagnosticsHandler (which is internal :( )
            Type type = typeof(HttpClientHandler).Assembly.GetType("System.Net.Http.DiagnosticsHandler");
            ConstructorInfo constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
            HttpMessageHandler diagnosticsHandler = (HttpMessageHandler)constructor.Invoke(new object[] { _httpHandler });

            _httpClient = new HttpClient(diagnosticsHandler);
        }

        public void Dispose()
        {
            _diagnosticsManager.Dispose();
        }

        [Fact]
        public async Task Creates_span()
        {
            await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values")));

            Assert.Single(_tracer.FinishedSpans());
        }

        [Fact]
        public async Task Span_is_child_of_parent()
        {
            // Create parent span
            using (var scope = _tracer.BuildSpan("parent").StartActive())
            {
                await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values")));
            }

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Equal(2, finishedSpans.Count);

            // Inner span is finished first
            var childSpan = finishedSpans[0];
            var parentSpan = finishedSpans[1];

            Assert.NotSame(parentSpan, childSpan);
            Assert.Single(childSpan.References);
            Assert.Equal(References.ChildOf, childSpan.References[0].ReferenceType);
            Assert.Same(parentSpan.Context, childSpan.References[0].Context);
        }

        [Fact]
        public async Task Span_has_correct_properties()
        {
            await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values")));

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Empty(span.GeneratedErrors);
            Assert.Empty(span.LogEntries);
            Assert.Equal("HTTP GET", span.OperationName);
            Assert.Null(span.ParentId);
            Assert.Empty(span.References);

            Assert.Equal(7, span.Tags.Count);
            Assert.Equal(Tags.SpanKindClient, span.Tags[Tags.SpanKind.Key]);
            Assert.Equal("HttpOut", span.Tags[Tags.Component.Key]);
            Assert.Equal("GET", span.Tags[Tags.HttpMethod.Key]);
            Assert.Equal("http://www.example.com/api/values", span.Tags[Tags.HttpUrl.Key]);
            Assert.Equal("www.example.com", span.Tags[Tags.PeerHostname.Key]);
            Assert.Equal(80, span.Tags[Tags.PeerPort.Key]);
            Assert.Equal(200, span.Tags[Tags.HttpStatus.Key]);
        }

        [Fact]
        public async Task Injects_trace_headers()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values"));

            await _httpClient.SendAsync(request);

            Assert.True(request.Headers.Contains("traceid"));
        }

        [Fact]
        public async Task Does_not_inject_trace_headers_if_disabled_in_options()
        {
            _options.InjectEnabled = req => !req.Properties.ContainsKey("ignore");

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values"));
            request.Properties["ignore"] = true;

            await _httpClient.SendAsync(request);

            Assert.False(request.Headers.Contains("traceid"));
        }

        [Fact]
        public async Task Ignores_requests_with_Ignore_property()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values"));
            request.Properties[HttpHandlerDiagnosticOptions.PropertyIgnore] = true;

            await _httpClient.SendAsync(request);

            Assert.Empty(_tracer.FinishedSpans());
        }

        [Fact]
        public async Task Ignores_requests_with_custom_rule()
        {
            _options.IgnorePatterns.Add(req => req.Properties.ContainsKey("foo"));

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values"));
            request.Properties["foo"] = 1;

            await _httpClient.SendAsync(request);

            Assert.Empty(_tracer.FinishedSpans());
        }

        [Fact]
        public async Task Calls_Options_OperationNameResolver()
        {
            _options.OperationNameResolver = _ => "foo";

            await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values")));

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal("foo", span.OperationName);
        }

        [Fact]
        public async Task Calls_Options_OnRequest()
        {
            bool onRequestCalled = false;

            _options.OnRequest = (_, __) => onRequestCalled = true;

            await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values")));

            Assert.True(onRequestCalled);
        }

        [Fact]
        public async Task Calls_Options_OnError()
        {
            bool onErrorCalled = false;

            _options.OnError = (_, __, ___) => onErrorCalled = true;

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values"));

            _httpHandler.OnSend = _ => throw new InvalidOperationException();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _httpClient.SendAsync(request));

            Assert.True(onErrorCalled);
        }

        [Fact]
        public async Task Creates_error_span_if_request_times_out()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values"));

            _httpHandler.OnSend = _ => throw new TaskCanceledException();

            await Assert.ThrowsAsync<TaskCanceledException>(() => _httpClient.SendAsync(request));

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.True(span.Tags[Tags.Error.Key] as bool?);
        }

        [Fact]
        public async Task Creates_error_span_if_request_fails()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://www.example.com/api/values"));

            _httpHandler.OnSend = _ => throw new InvalidOperationException();

            await Assert.ThrowsAsync<InvalidOperationException>(() => _httpClient.SendAsync(request));

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.True(span.Tags[Tags.Error.Key] as bool?);
        }
    }
}
