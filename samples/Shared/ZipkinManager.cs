using System;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Logging;
using OpenTracing.Tracer.Zipkin;
using OpenTracing.Util;
using zipkin4net;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport;
using zipkin4net.Transport.Http;

namespace Shared
{
    public class ZipkinManager : zipkin4net.ILogger, IDisposable
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public ZipkinManager(ILogger<ZipkinManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            // Zipkin Configuration

            var zipkinHttpClient = new HttpClient(new SetOtIgnoreHandler
            {
                InnerHandler = new HttpClientHandler()
            });

            var zipkinSender = new HttpZipkinSender(zipkinHttpClient, "http://localhost:9411", "application/json");
            var zipkinTracer = new ZipkinTracer(zipkinSender, new JSONSpanSerializer());

            TraceManager.SamplingRate = 1.0f;

            TraceManager.RegisterTracer(zipkinTracer);
            TraceManager.Start(this);

            // OpenTracing -> Zipkin Configuration

            string serviceName = Assembly.GetEntryAssembly().GetName().Name;
            var otTracer = new OtTracer(
               serviceName,
               new AsyncLocalScopeManager(),
               new ZipkinHttpTraceInjector(),
               new ZipkinHttpTraceExtractor());

            GlobalTracer.Register(otTracer);
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }

        public void Dispose()
        {
            TraceManager.Stop();
        }
    }
}
