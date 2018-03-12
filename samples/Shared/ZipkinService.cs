using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTracing.Tracer.Zipkin;
using OpenTracing.Util;
using zipkin4net;
using zipkin4net.Propagation;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;

namespace Shared
{
    public class ZipkinService : IHostedService, zipkin4net.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public ZipkinService(ILogger<ZipkinService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Zipkin Configuration

            var zipkinHttpClient = new HttpClient(new SetOtIgnoreHandler
            {
                InnerHandler = new HttpClientHandler()
            });

            var zipkinSender = new HttpZipkinSender(zipkinHttpClient, "http://localhost:9411", "application/json");
            var zipkinTracer = new ZipkinTracer(zipkinSender, new JSONSpanSerializer(), new Statistics());

            TraceManager.SamplingRate = 1.0f;

            TraceManager.RegisterTracer(zipkinTracer);
            TraceManager.Start(this);

            // OpenTracing -> Zipkin Configuration

            string serviceName = Assembly.GetEntryAssembly().GetName().Name;
            var otTracer = new OtTracer(
               serviceName,
               new AsyncLocalScopeManager(),
               Propagations.B3String);

            GlobalTracer.Register(otTracer);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            TraceManager.Stop();

            return Task.CompletedTask;
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
    }
}
