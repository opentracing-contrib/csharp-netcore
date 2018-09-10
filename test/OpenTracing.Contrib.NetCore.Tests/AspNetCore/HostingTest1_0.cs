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
    public class HostingTest1_0 : IDisposable
    {
        private readonly MockTracer _tracer;

        private readonly IServiceProvider _serviceProvider;
        private readonly FeatureCollection _features;
        private readonly DiagnosticManager _diagnosticManager;
        private readonly HostingApplication _hostingApplication;
        private readonly DefaultHttpContext _httpContext;

        public HostingTest1_0(ITestOutputHelper output)
        {
            _tracer = new MockTracer();

            var aspNetCoreOptions = new AspNetCoreDiagnosticOptions();

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

            request.Protocol = "HTTP/1.0";
            request.Method = HttpMethods.Get;
            request.Scheme = requestUri.Scheme;
            // HTTP/1.0 requests are not required to provide a Host in the request
            request.Host = new HostString();
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
            Assert.Equal($"http://{HostingEventProcessor.NoHostSpecified}/foo", span.Tags[Tags.HttpUrl.Key]);
            Assert.Equal(200, span.Tags[Tags.HttpStatus.Key]);
        }
    }
}
