using System;
using System.Collections.Generic;
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
    internal sealed class HttpOutInterceptor : DiagnosticInterceptor
    {
        // Diagnostic names:
        // https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs
        public const string ActivityName = "System.Net.Http.HttpRequestOut";
        public const string EventRequest = ActivityName + ".Start";
        public const string EventResponse = ActivityName + ".Stop";
        public const string EventException = "System.Net.Http.Exception";

        private readonly HttpOutOptions _options;

        public HttpOutInterceptor(ILoggerFactory loggerFactory, ITracer tracer, IOptions<HttpOutOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override bool IsEnabled(string listenerName)
        {
            if (listenerName == ActivityName)
                return true;
            if (listenerName == EventRequest)
                return true;
            if (listenerName == EventResponse)
                return true;
            if (listenerName == EventException)
                return true;

            return false;
        }

        [DiagnosticName(ActivityName)]
        public void OnActivity()
        {
            // HACK: There must be a method for the main activity name otherwise no activities are logged.
            // So this is just a no-op.
        }

        [DiagnosticName(EventRequest)]
        public void OnRequest(HttpRequestMessage request)
        {
            Execute(() =>
            {
                if (ShouldIgnore(request))
                {
                    Logger.LogDebug("Ignoring Request {RequestUri}", request.RequestUri);
                    return;
                }

                string operationName = _options.OperationNameResolver(request);

                IScope scope = Tracer.BuildSpan(operationName)
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindClient)
                    .WithTag(Tags.Component.Key, _options.ComponentName)
                    .WithTag(Tags.HttpMethod.Key, request.Method.ToString())
                    .WithTag(Tags.HttpUrl.Key, request.RequestUri.ToString())
                    .WithTag(Tags.PeerHostname.Key, request.RequestUri.Host)
                    .WithTag(Tags.PeerPort.Key, request.RequestUri.Port)
                    .StartActive(finishSpanOnDispose: true);

                _options.OnRequest?.Invoke(scope.Span, request);

                Tracer.Inject(scope.Span.Context, BuiltinFormats.HttpHeaders, new HttpHeadersInjectAdapter(request.Headers));
            });
        }

        [DiagnosticName(EventException)]
        public void OnException(HttpRequestMessage request, Exception exception)
        {
            Execute(() =>
            {
                Tracer.ActiveSpan?.SetException(exception);
            });
        }

        [DiagnosticName(EventResponse)]
        public void OnResponse(HttpResponseMessage response, TaskStatus requestTaskStatus)
        {
            Execute(() =>
            {
                IScope scope = Tracer.ScopeManager.Active;

                if (response != null)
                {
                    scope?.Span?.SetTag(Tags.HttpStatus.Key, (int)response.StatusCode);
                }

                if (requestTaskStatus == TaskStatus.Canceled || requestTaskStatus == TaskStatus.Faulted)
                {
                    scope?.Span?.SetTag(Tags.Error.Key, true);
                }

                scope?.Dispose();
            });
        }

        private bool ShouldIgnore(HttpRequestMessage request)
        {
            foreach (Func<HttpRequestMessage, bool> shouldIgnore in _options.ShouldIgnore)
            {
                if (shouldIgnore(request))
                    return true;
            }

            return false;
        }
    }
}
