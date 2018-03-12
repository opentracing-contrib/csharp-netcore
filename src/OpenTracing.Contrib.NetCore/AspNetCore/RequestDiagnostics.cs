using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    /// <summary>
    /// Instruments incoming HTTP requests.
    /// <para/>See https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/Internal/HostingApplicationDiagnostics.cs
    /// </summary>
    internal sealed class RequestDiagnostics : DiagnosticSubscriberWithObserver
    {
        public const string DiagnosticListenerName = "Microsoft.AspNetCore";

        public const string EventActivity = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
        public const string EventActivityStart = EventActivity + ".Start";
        public const string EventActivityStop = EventActivity + ".Stop";
        public const string EventUnhandledException = "Microsoft.AspNetCore.Hosting.UnhandledException";

        public static readonly Action<GenericDiagnosticOptions> GenericDiagnosticsExclusions = options =>
        {
            options.IgnoreEvent(DiagnosticListenerName, EventActivity);
            options.IgnoreEvent(DiagnosticListenerName, EventActivityStart);
            options.IgnoreEvent(DiagnosticListenerName, EventActivityStop);
            options.IgnoreEvent(DiagnosticListenerName, EventUnhandledException);

            // Deprecated Hosting events
            options.IgnoreEvent(DiagnosticListenerName, "Microsoft.AspNetCore.Hosting.BeginRequest");
            options.IgnoreEvent(DiagnosticListenerName, "Microsoft.AspNetCore.Hosting.EndRequest");
        };

        private readonly PropertyFetcher _activityStart_HttpContextFetcher = new PropertyFetcher("HttpContext");
        private readonly PropertyFetcher _activityStop_HttpContextFetcher = new PropertyFetcher("HttpContext");
        private readonly PropertyFetcher _unhandledException_ExceptionFetcher = new PropertyFetcher("exception");

        private readonly RequestDiagnosticOptions _options;

        protected override string ListenerName => DiagnosticListenerName;

        public RequestDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<RequestDiagnosticOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override void OnNextCore(string eventName, object arg)
        {
            switch (eventName)
            {
                case EventActivityStart:
                    {
                        var httpContext = (HttpContext)_activityStart_HttpContextFetcher.Fetch(arg);

                        if (ShouldIgnore(httpContext))
                        {
                            Logger.LogDebug("Ignoring request");
                            return;
                        }

                        var request = httpContext.Request;

                        ISpanContext extractedSpanContext = null;

                        if (_options.ExtractEnabled?.Invoke(httpContext) ?? true)
                        {
                            extractedSpanContext = Tracer.Extract(BuiltinFormats.HttpHeaders, new RequestHeadersExtractAdapter(request.Headers));
                        }

                        string operationName = _options.OperationNameResolver(httpContext);

                        IScope scope = Tracer.BuildSpan(operationName)
                            .AsChildOf(extractedSpanContext)
                            .WithTag(Tags.Component.Key, _options.ComponentName)
                            .WithTag(Tags.SpanKind.Key, Tags.SpanKindServer)
                            .WithTag(Tags.HttpMethod.Key, request.Method)
                            .WithTag(Tags.HttpUrl.Key, request.GetDisplayUrl())
                            .StartActive(finishSpanOnDispose: true);

                        _options.OnRequest?.Invoke(scope.Span, httpContext);
                    }
                    break;

                case EventUnhandledException:
                    {
                        ISpan span = Tracer.ActiveSpan;
                        if (span != null)
                        {
                            var exception = (Exception)_unhandledException_ExceptionFetcher.Fetch(arg);
                            span.SetException(exception);
                        }
                    }
                    break;

                case EventActivityStop:
                    {
                        IScope scope = Tracer.ScopeManager.Active;
                        if (scope != null)
                        {
                            var httpContext = (HttpContext)_activityStop_HttpContextFetcher.Fetch(arg);

                            Tags.HttpStatus.Set(scope.Span, httpContext.Response.StatusCode);
                            scope.Dispose();
                        }
                    }
                    break;
            }
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
