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
    internal sealed class RequestInterceptor : DiagnosticInterceptor
    {
        // https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/Internal/HostingApplicationDiagnostics.cs
        private const string EventBeginRequest = "Microsoft.AspNetCore.Hosting.BeginRequest";
        private const string EventEndRequest = "Microsoft.AspNetCore.Hosting.EndRequest";
        private const string EventUnhandledException = "Microsoft.AspNetCore.Hosting.UnhandledException";

        private const string ItemsKey = "ot-RequestScope";

        private readonly RequestInOptions _options;

        // https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/WebHostBuilder.cs
        protected override string ListenerName => "Microsoft.AspNetCore";

        public RequestInterceptor(ILoggerFactory loggerFactory, ITracer tracer, IOptions<RequestInOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [DiagnosticName(EventBeginRequest)]
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

                ISpanContext extractedSpanContext = Tracer.Extract(BuiltinFormats.HttpHeaders, new HeaderDictionaryCarrier(request.Headers));

                IScope scope = Tracer.BuildSpan(_options.OperationNameResolver(httpContext))
                    .AsChildOf(extractedSpanContext)
                    .WithTag(Tags.Component.Key, _options.ComponentName)
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindServer)
                    .WithTag(Tags.HttpMethod.Key, request.Method)
                    .WithTag(Tags.HttpUrl.Key, request.GetDisplayUrl())
                    .StartActive(finishSpanOnDispose: true);

                // We don't rely on the ScopeManager as someone could have messed up the disposal-chain in ScopeManager.
                httpContext.Items[ItemsKey] = scope;

                _options.OnRequest?.Invoke(scope.Span, httpContext);
            });
        }

        /// <summary>
        /// This event is called for requests that did NOT end in an exception.
        /// </summary>
        [DiagnosticName(EventEndRequest)]
        public void OnEndRequest(HttpContext httpContext)
        {
            ExecuteOnScope(httpContext, scope =>
            {
                Tags.HttpStatus.Set(scope.Span, httpContext.Response.StatusCode);
                scope.Dispose();
            });
        }

        /// <summary>
        /// This event is called for requests that DID end in an exception.
        /// </summary>
        [DiagnosticName(EventUnhandledException)]
        public void OnUnhandledException(HttpContext httpContext, Exception exception)
        {
            ExecuteOnScope(httpContext, scope =>
            {
                scope.Span.SetException(exception);
                Tags.HttpStatus.Set(scope.Span, httpContext.Response.StatusCode);
                scope.Dispose();
            });
        }

        private bool ShouldIgnore(HttpContext httpContext)
        {
            foreach (Func<HttpContext, bool> shouldIgnore in _options.ShouldIgnore)
            {
                if (shouldIgnore(httpContext))
                    return true;
            }

            return false;
        }

        private void ExecuteOnScope(HttpContext httpContext, Action<IScope> action)
        {
            Execute(() =>
            {
                if (httpContext.Items.TryGetValue(ItemsKey, out object objScope) && objScope is IScope scope)
                {
                    action(scope);
                }
                else
                {
                    Logger.LogError("Scope not found");
                }
            });
        }
    }
}
