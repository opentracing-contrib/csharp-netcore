using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers.AspNetCore
{
    /// <summary>
    /// Instruments incoming HTTP requests.
    /// <para/>See https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/Internal/HostingApplicationDiagnostics.cs
    /// </summary>
    internal sealed class RequestDiagnosticSubscriber : DiagnosticSubscriberWithAdapter
    {
        public const string DiagnosticListenerName = "Microsoft.AspNetCore";
        public const string EventOnActivity = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
        public const string EventOnActivityStart = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";
        public const string EventOnActivityStop = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";
        public const string EventOnUnhandledException = "Microsoft.AspNetCore.Hosting.UnhandledException";

        private readonly RequestDiagnosticOptions _options;

        protected override string ListenerName => DiagnosticListenerName;

        public RequestDiagnosticSubscriber(ILoggerFactory loggerFactory, ITracer tracer, IOptions<AspNetCoreOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value?.RequestDiagnostic ?? throw new ArgumentNullException(nameof(options));
        }

        [DiagnosticName(EventOnActivity)]
        public void OnActivity()
        {
            // HACK: There must be a method for the main activity name otherwise no activities are logged.
            // So this is just a no-op.
            // https://github.com/aspnet/Home/issues/2325
        }

        [DiagnosticName(EventOnActivityStart)]
        public void OnActivityStart(HttpContext httpContext)
        {
            Execute(() =>
            {
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
            });
        }

        [DiagnosticName(EventOnUnhandledException)]
        public void OnUnhandledException(HttpContext httpContext, Exception exception)
        {
            Execute(() =>
            {
                Tracer.ActiveSpan?.SetException(exception);
            });
        }

        [DiagnosticName(EventOnActivityStop)]
        public void OnActivityStop(HttpContext httpContext)
        {
            Execute(() =>
            {
                IScope scope = Tracer.ScopeManager.Active;
                if (scope != null)
                {
                    Tags.HttpStatus.Set(scope.Span, httpContext.Response.StatusCode);
                    scope.Dispose();
                }
            });
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
