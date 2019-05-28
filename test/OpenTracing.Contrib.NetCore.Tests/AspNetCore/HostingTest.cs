using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenTracing.Contrib.NetCore.AspNetCore;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Mock;
using OpenTracing.Tag;
using Xunit;
using Xunit.Abstractions;

namespace OpenTracing.Contrib.NetCore.Tests.AspNetCore
{
    [Collection("DiagnosticSource") /* All DiagnosticSource tests must be in the same collection to ensure they are NOT run in parallel. */]
    public class HostingTest : IDisposable
    {
        private readonly MockTracer _tracer;
        private readonly HostingOptions _options;

        private readonly IServiceProvider _serviceProvider;
        private readonly FeatureCollection _features;
        private readonly DiagnosticManager _diagnosticManager;
        private readonly HostingApplication _hostingApplication;
        private readonly DefaultHttpContext _httpContext;

        public HostingTest(ITestOutputHelper output)
        {
            _tracer = new MockTracer();

            var aspNetCoreOptions = new AspNetCoreDiagnosticOptions();
            _options = aspNetCoreOptions.Hosting;

            _serviceProvider = new ServiceCollection()
                .AddLogging(logging =>
                {
                    logging.AddXunit(output);
                    logging.AddFilter("OpenTracing", LogLevel.Trace);
                })
                .AddOpenTracingCoreServices(builder =>
                {
                    builder.AddAspNetCore();

                    builder.Services.AddSingleton<ITracer>(_tracer);
                    builder.Services.AddSingleton(Options.Create(aspNetCoreOptions));
                })
                .BuildServiceProvider();

            _diagnosticManager = _serviceProvider.GetRequiredService<DiagnosticManager>();
            _diagnosticManager.Start();

            // Request

            _httpContext = new DefaultHttpContext();
            SetRequest();

            // Hosting Application

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");

            _features = new FeatureCollection();
            _features.Set<IHttpRequestFeature>(new HttpRequestFeature());

            var httpContextFactory = Substitute.For<IHttpContextFactory>();
            httpContextFactory.Create(_features).Returns(_httpContext);

            _hostingApplication = new HostingApplication(
                ctx => Task.FromResult(0),
                _serviceProvider.GetRequiredService<ILogger<HostingTest>>(),
                diagnosticSource,
                httpContextFactory);
        }

        public void Dispose()
        {
            _diagnosticManager.Dispose();
            (_serviceProvider as IDisposable).Dispose();
        }

        private void SetRequest()
        {
            var request = _httpContext.Request;

            Uri requestUri = new Uri("http://www.example.com/foo");

            request.Protocol = "HTTP/1.1";
            request.Method = HttpMethods.Get;
            request.Scheme = requestUri.Scheme;
            request.Host = HostString.FromUriComponent(requestUri);
            if (requestUri.IsDefaultPort)
            {
                request.Host = new HostString(request.Host.Host);
            }
            request.PathBase = PathString.Empty;
            request.Path = PathString.FromUriComponent(requestUri);
            request.QueryString = QueryString.FromUriComponent(requestUri);
        }

        private async Task ExecuteRequestAsync(Exception exception = null)
        {
            var context = _hostingApplication.CreateContext(_features);
            await _hostingApplication.ProcessRequestAsync(context);
            _hostingApplication.DisposeContext(context, exception);
        }

        [Fact]
        public async Task Request_creates_span()
        {
            await ExecuteRequestAsync();

            var finishedSpans = _tracer.FinishedSpans();

            Assert.Single(finishedSpans);
        }

        [Fact]
        public async Task Span_has_correct_properties()
        {
            await ExecuteRequestAsync();

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Empty(span.GeneratedErrors);
            Assert.Empty(span.LogEntries);
            Assert.Equal("HTTP GET", span.OperationName);
            Assert.Null(span.ParentId);
            Assert.Empty(span.References);

            Assert.Equal(5, span.Tags.Count);
            Assert.Equal(Tags.SpanKindServer, span.Tags[Tags.SpanKind.Key]);
            Assert.Equal("HttpIn", span.Tags[Tags.Component.Key]);
            Assert.Equal("GET", span.Tags[Tags.HttpMethod.Key]);
            Assert.Equal("http://www.example.com/foo", span.Tags[Tags.HttpUrl.Key]);
            Assert.Equal(200, span.Tags[Tags.HttpStatus.Key]);
        }

        [Fact]
        public async Task Span_has_status_404()
        {
            _httpContext.Response.StatusCode = 404;

            await ExecuteRequestAsync();

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Equal(404, span.Tags[Tags.HttpStatus.Key]);
        }

        [Fact]
        public async Task Extracts_trace_headers()
        {
            _httpContext.Request.Headers.Add("traceid", "100");
            _httpContext.Request.Headers.Add("spanid", "101");

            await ExecuteRequestAsync();

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
            _httpContext.Request.Headers.Add("ignore", "1");

            _options.ExtractEnabled = context => !context.Request.Headers.ContainsKey("ignore");

            await ExecuteRequestAsync();

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.Empty(span.References);
        }

        [Fact]
        public async Task Ignores_requests_with_custom_rule()
        {
            _httpContext.Request.Headers.Add("ignore", "1");

            _options.IgnorePatterns.Add(context => context.Request.Headers.ContainsKey("ignore"));

            await ExecuteRequestAsync();

            Assert.Empty(_tracer.FinishedSpans());
        }

        [Fact]
        public async Task Calls_Options_OperationNameResolver()
        {
            _options.OperationNameResolver = _ => "test";

            await ExecuteRequestAsync();

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

            await ExecuteRequestAsync();

            Assert.True(onRequestCalled);
        }

        [Fact]
        public async Task Calls_Options_OnError()
        {
            bool onErrorCalled = false;
            _options.OnError = (_, __, ___) => onErrorCalled = true;

            var exception = new InvalidOperationException("You shall not pass");
            await ExecuteRequestAsync(exception);

            Assert.True(onErrorCalled);
        }

        [Fact]
        public async Task Creates_error_span_if_request_throws_exception()
        {
            var exception = new InvalidOperationException("You shall not pass");
            await ExecuteRequestAsync(exception);

            var finishedSpans = _tracer.FinishedSpans();
            Assert.Single(finishedSpans);

            var span = finishedSpans[0];
            Assert.True(span.Tags[Tags.Error.Key] as bool?);

            var logs = span.LogEntries;
            Assert.Single(logs);
            Assert.Equal("error", logs[0].Fields[LogFields.Event]);
            Assert.Equal("InvalidOperationException", logs[0].Fields[LogFields.ErrorKind]);
        }
    }
}
