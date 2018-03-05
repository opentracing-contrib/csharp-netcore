using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.Interceptors.HttpOut
{
    /// <summary>
    /// Instruments outgoing HTTP calls that use <see cref="HttpClientHandler"/>.
    /// <para/>See https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandler.cs
    /// <para/>and https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs
    /// </summary>
    /// 
    internal sealed class HttpOutInterceptor : DiagnosticInterceptor
    {
        private const string PropertiesKey = "ot-Span";

        private readonly HttpOutOptions _options;

        protected override string ListenerName => "HttpHandlerDiagnosticListener";

        public HttpOutInterceptor(ILoggerFactory loggerFactory, ITracer tracer, IOptions<HttpOutOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [DiagnosticName("System.Net.Http.HttpRequestOut")]
        public void OnActivity()
        {
            // HACK: There must be a method for the main activity name otherwise no activities are logged.
            // So this is just a no-op.
            // https://github.com/aspnet/Home/issues/2325
        }

        [DiagnosticName("System.Net.Http.HttpRequestOut.Start")]
        public void OnStart(HttpRequestMessage request)
        {
            Execute(() =>
            {
                if (IgnoreRequest(request))
                {
                    Logger.LogDebug("Ignoring Request {RequestUri}", request.RequestUri);
                    return;
                }

                ISpan span = Tracer.BuildSpan(_options.OperationNameResolver(request))
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindClient)
                    .WithTag(Tags.Component.Key, _options.ComponentName)
                    .WithTag(Tags.HttpMethod.Key, request.Method.ToString())
                    .WithTag(Tags.HttpUrl.Key, request.RequestUri.ToString())
                    .WithTag(Tags.PeerHostname.Key, request.RequestUri.Host)
                    .WithTag(Tags.PeerPort.Key, request.RequestUri.Port)
                    .Start();

                _options.OnRequest?.Invoke(span, request);

                if (_options.InjectEnabled?.Invoke(request) ?? true)
                {
                    Tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new HttpHeadersInjectAdapter(request.Headers));
                }

                // This throws if there's already an item with the same key. We do this for now to get notified of potential bugs.
                request.Properties.Add(PropertiesKey, span);
            });
        }

        [DiagnosticName("System.Net.Http.Exception")]
        public void OnException(HttpRequestMessage request, Exception exception)
        {
            Execute(() =>
            {
                if (request.Properties.TryGetValue(PropertiesKey, out object objSpan) && objSpan is ISpan span)
                {
                    span.SetException(exception);
                }
            });
        }

        [DiagnosticName("System.Net.Http.HttpRequestOut.Stop")]
        public void OnStop(HttpResponseMessage response, HttpRequestMessage request, TaskStatus requestTaskStatus)
        {
            Execute(() =>
            {
                if (request.Properties.TryGetValue(PropertiesKey, out object objSpan) && objSpan is ISpan span)
                {
                    if (response != null)
                    {
                        span.SetTag(Tags.HttpStatus.Key, (int)response.StatusCode);
                    }

                    if (requestTaskStatus == TaskStatus.Canceled || requestTaskStatus == TaskStatus.Faulted)
                    {
                        span.SetTag(Tags.Error.Key, true);
                    }

                    span.Finish();

                    request.Properties[PropertiesKey] = null;
                }
            });
        }

        private bool IgnoreRequest(HttpRequestMessage request)
        {
            foreach (Func<HttpRequestMessage, bool> ignore in _options.IgnorePatterns)
            {
                if (ignore(request))
                    return true;
            }

            return false;
        }
    }
}
