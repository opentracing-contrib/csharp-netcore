using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenTracing.Contrib.NetCore.Internal;

namespace OpenTracing.Contrib.NetCore.Benchmarks.AspNetCore
{
    public class RequestDiagnosticsBenchmark
    {
        private IServiceProvider _serviceProvider;
        private FeatureCollection _features;
        private HostingApplication _hostingApplication;
        private DefaultHttpContext _httpContext;

        [Params(InstrumentationMode.None, InstrumentationMode.Noop, InstrumentationMode.Mock)]
        public InstrumentationMode Mode { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _serviceProvider = new ServiceCollection()
                .AddLogging()
                .AddOpenTracingCoreServices(builder =>
                {
                    builder.AddAspNetCore();
                    builder.AddBenchmarkTracer(Mode);
                })
                .BuildServiceProvider();

            var diagnosticManager = _serviceProvider.GetRequiredService<DiagnosticManager>();
            diagnosticManager.Start();

            // Request

            _httpContext = new DefaultHttpContext();
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

            // Hosting Application

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");

            _features = new FeatureCollection();
            _features.Set<IHttpRequestFeature>(new HttpRequestFeature());

            var httpContextFactory = Substitute.For<IHttpContextFactory>();
            httpContextFactory.Create(_features).Returns(_httpContext);

            _hostingApplication = new HostingApplication(
                ctx => Task.FromResult(0),
                _serviceProvider.GetRequiredService<ILogger<RequestDiagnosticsBenchmark>>(),
                diagnosticSource,
                httpContextFactory);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            (_serviceProvider as IDisposable).Dispose();
        }

        [Benchmark]
        public async Task GetAsync()
        {
            var context = _hostingApplication.CreateContext(_features);
            await _hostingApplication.ProcessRequestAsync(context);
            _hostingApplication.DisposeContext(context, null);
        }
    }
}
