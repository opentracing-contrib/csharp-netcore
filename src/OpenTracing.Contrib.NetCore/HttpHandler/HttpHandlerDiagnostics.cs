using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.HttpHandler
{
    /// <summary>
    /// Instruments outgoing HTTP calls that use <see cref="HttpClientHandler"/>.
    /// <para/>See https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandler.cs
    /// <para/>and https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs
    /// </summary>
    internal sealed class HttpHandlerDiagnostics : DiagnosticEventObserver
    {
        public const string DiagnosticListenerName = "HttpHandlerDiagnosticListener";

        private const string PropertiesKey = "ot-Span";

        private static readonly PropertyFetcher _activityStart_RequestFetcher = new PropertyFetcher("Request");
        private static readonly PropertyFetcher _activityStop_RequestFetcher = new PropertyFetcher("Request");
        private static readonly PropertyFetcher _activityStop_ResponseFetcher = new PropertyFetcher("Response");
        private static readonly PropertyFetcher _activityStop_RequestTaskStatusFetcher = new PropertyFetcher("RequestTaskStatus");
        private static readonly PropertyFetcher _exception_RequestFetcher = new PropertyFetcher("Request");
        private static readonly PropertyFetcher _exception_ExceptionFetcher = new PropertyFetcher("Exception");

        private readonly HttpHandlerDiagnosticOptions _options;

        public HttpHandlerDiagnostics(ILoggerFactory loggerFactory, ITracer tracer,
            IOptions<HttpHandlerDiagnosticOptions> options)
            : base(loggerFactory, tracer, options?.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        protected override bool IsSupportedEvent(string eventName)
        {
            return eventName switch
            {
                // We don't want to get the old deprecated events.
                // https://github.com/dotnet/runtime/blob/master/src/libraries/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs
                "System.Net.Http.Request" => false,
                "System.Net.Http.Response" => false,
                _ => true,
            };
        }

        protected override IEnumerable<string> HandledEventNames()
        {
            yield return "System.Net.Http.HttpRequestOut.Start";
            yield return "System.Net.Http.Exception";
            yield return "System.Net.Http.HttpRequestOut.Stop";
        }

        protected override void HandleEvent(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "System.Net.Http.HttpRequestOut.Start":
                    {
                        var request = (HttpRequestMessage)_activityStart_RequestFetcher.Fetch(untypedArg);

                        var activeSpan = Tracer.ActiveSpan;

                        if (activeSpan == null && !_options.StartRootSpans)
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring Request due to missing parent span");
                            }
                            return;
                        }

                        if (IgnoreRequest(request))
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring Request {RequestUri}", request.RequestUri);
                            }
                            return;
                        }

                        string operationName = _options.OperationNameResolver(request);

                        ISpan span = Tracer.BuildSpan(operationName)
                            .AsChildOf(activeSpan)
                            .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                            .WithTag(Tags.Component, _options.ComponentName)
                            .WithTag(Tags.HttpMethod, request.Method.ToString())
                            .WithTag(Tags.HttpUrl, request.RequestUri.ToString())
                            .WithTag(Tags.PeerHostname, request.RequestUri.Host)
                            .WithTag(Tags.PeerPort, request.RequestUri.Port)
                            .Start();

                        _options.OnRequest?.Invoke(span, request);

                        if (_options.InjectEnabled?.Invoke(request) ?? true)
                        {
                            Tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new HttpHeadersInjectAdapter(request.Headers));
                        }

                        var requestOptions = GetRequestOptions(request);
                        requestOptions[PropertiesKey] = span;
                    }
                    break;

                case "System.Net.Http.Exception":
                    {
                        var request = (HttpRequestMessage)_exception_RequestFetcher.Fetch(untypedArg);
                        var requestOptions = GetRequestOptions(request);

                        if (requestOptions.TryGetValue(PropertiesKey, out object objSpan) && objSpan is ISpan span)
                        {
                            var exception = (Exception)_exception_ExceptionFetcher.Fetch(untypedArg);

                            span.SetException(exception);

                            _options.OnError?.Invoke(span, exception, request);
                        }
                    }
                    break;

                case "System.Net.Http.HttpRequestOut.Stop":
                    {
                        var request = (HttpRequestMessage)_activityStop_RequestFetcher.Fetch(untypedArg);
                        var requestOptions = GetRequestOptions(request);

                        if (requestOptions.TryGetValue(PropertiesKey, out object objSpan) && objSpan is ISpan span)
                        {
                            requestOptions.Remove(PropertiesKey);

                            var response = (HttpResponseMessage)_activityStop_ResponseFetcher.Fetch(untypedArg);
                            var requestTaskStatus = (TaskStatus)_activityStop_RequestTaskStatusFetcher.Fetch(untypedArg);

                            if (response != null)
                            {
                                span.SetTag(Tags.HttpStatus, (int)response.StatusCode);
                            }

                            if (requestTaskStatus == TaskStatus.Canceled || requestTaskStatus == TaskStatus.Faulted)
                            {
                                span.SetTag(Tags.Error, true);
                            }

                            span.Finish();
                        }
                    }
                    break;

                default:
                    HandleUnknownEvent(eventName, untypedArg);
                    break;
            }
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

        private IDictionary<string, object> GetRequestOptions(HttpRequestMessage request)
        {
            IDictionary<string, object> requestOptions;

#if NETCOREAPP2_1 || NETCOREAPP3_1
            requestOptions = request.Properties;
#else 
            requestOptions = request.Options;
#endif

            return requestOptions;
        }
    }
}
