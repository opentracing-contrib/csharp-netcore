using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.AspNetCore.Configuration;
using OpenTracing.Contrib.NetCore;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.AspNetCore.Interceptors.RequestIn
{
    /// <summary>
    /// Instruments incoming HTTP requests.
    /// <para/>See https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/Internal/HostingApplicationDiagnostics.cs
    /// </summary>
    internal sealed class RequestInterceptor : DiagnosticInterceptor
    {
        private readonly RequestInOptions _options;

        // https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/WebHostBuilder.cs
        protected override string ListenerName => "Microsoft.AspNetCore";

        public RequestInterceptor(ILoggerFactory loggerFactory, ITracer tracer, IOptions<RequestInOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
        public void OnActivity()
        {
            // HACK: There must be a method for the main activity name otherwise no activities are logged.
            // So this is just a no-op.
            // https://github.com/aspnet/Home/issues/2325
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")]
        public void OnBeginRequest(HttpContext httpContext)
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
                    extractedSpanContext = Tracer.Extract(BuiltinFormats.HttpHeaders, new HeadersExtractAdapter(request.Headers));
                }

                IScope scope = Tracer.BuildSpan(_options.OperationNameResolver(httpContext))
                    .AsChildOf(extractedSpanContext)
                    .WithTag(Tags.Component.Key, _options.ComponentName)
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindServer)
                    .WithTag(Tags.HttpMethod.Key, request.Method)
                    .WithTag(Tags.HttpUrl.Key, request.GetDisplayUrl())
                    .StartActive(finishSpanOnDispose: true);

                _options.OnRequest?.Invoke(scope.Span, httpContext);
            });
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.UnhandledException")]
        public void OnUnhandledException(HttpContext httpContext, Exception exception)
        {
            Execute(() =>
            {
                Tracer.ActiveSpan?.SetException(exception);
            });
        }

        [DiagnosticName("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")]
        public void OnEndRequest(HttpContext httpContext)
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
