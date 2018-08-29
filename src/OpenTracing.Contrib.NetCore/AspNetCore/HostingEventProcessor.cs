using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    internal class HostingEventProcessor
    {
        private static readonly PropertyFetcher _httpRequestIn_start_HttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher _httpRequestIn_stop_HttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher _unhandledException_ExceptionFetcher = new PropertyFetcher("exception");

        internal static readonly string NoHostSpecified = String.Empty;

        private readonly ITracer _tracer;
        private readonly ILogger _logger;
        private readonly HostingOptions _options;

        public HostingEventProcessor(ITracer tracer, ILogger logger, HostingOptions options)
        {
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool ProcessEvent(string eventName, object arg)
        {
            switch (eventName)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    {
                        var httpContext = (HttpContext)_httpRequestIn_start_HttpContextFetcher.Fetch(arg);

                        if (ShouldIgnore(httpContext))
                        {
                            _logger.LogDebug("Ignoring request");
                        }
                        else
                        {
                            var request = httpContext.Request;

                            ISpanContext extractedSpanContext = null;

                            if (_options.ExtractEnabled?.Invoke(httpContext) ?? true)
                            {
                                extractedSpanContext = _tracer.Extract(BuiltinFormats.HttpHeaders, new RequestHeadersExtractAdapter(request.Headers));
                            }

                            string operationName = _options.OperationNameResolver(httpContext);

                            IScope scope = _tracer.BuildSpan(operationName)
                                .AsChildOf(extractedSpanContext)
                                .WithTag(Tags.Component, _options.ComponentName)
                                .WithTag(Tags.SpanKind, Tags.SpanKindServer)
                                .WithTag(Tags.HttpMethod, request.Method)
                                .WithTag(Tags.HttpUrl, GetDisplayUrl(request))
                                .StartActive();

                            _options.OnRequest?.Invoke(scope.Span, httpContext);
                        }
                    }
                    return true;

                case "Microsoft.AspNetCore.Hosting.UnhandledException":
                    {
                        ISpan span = _tracer.ActiveSpan;
                        if (span != null)
                        {
                            var exception = (Exception)_unhandledException_ExceptionFetcher.Fetch(arg);

                            span.SetException(exception);
                        }
                    }
                    return true;

                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    {
                        IScope scope = _tracer.ScopeManager.Active;
                        if (scope != null)
                        {
                            var httpContext = (HttpContext)_httpRequestIn_stop_HttpContextFetcher.Fetch(arg);

                            scope.Span.SetTag(Tags.HttpStatus, httpContext.Response.StatusCode);
                            scope.Dispose();
                        }
                    }
                    return true;

                default: return false;
            }
        }

        private static string GetDisplayUrl(HttpRequest request)
        {
            if (request.Host.HasValue)
            {
                return request.GetDisplayUrl();
            }

            // HTTP 1.0 requests are not required to provide a Host to be valid
            // Since this is just for display, we can provide a string that is
            // not an actual Uri with only the fields that are specified.
            // request.GetDisplayUrl(), used above, will throw an exception
            // if request.Host is null.
            return $"{request.Scheme}://{NoHostSpecified}{request.PathBase.Value}{request.Path.Value}{request.QueryString.Value}";
        }

        private bool ShouldIgnore(HttpContext httpContext)
        {
            foreach (Func<HttpContext, bool> ignore in _options.IgnorePatterns)
            {
                if (ignore(httpContext))
                    return true;
            }

            return false;
        }
    }
}
