using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.AspNetCore.Interceptors.RequestIn
{
    internal sealed class RequestInterceptor : DiagnosticInterceptor
    {
        // Events from:
        // - Microsoft.AspNetCore.Hosting -> HostingApplication,
        // - Microsoft.AspNetCore.Diagnostics -> ExceptionHandlerMiddleware,
        // - Microsoft.AspNetCore.Diagnostics -> DeveloperExceptionPageMiddleware
        private const string EventBeginRequest = "Microsoft.AspNetCore.Hosting.BeginRequest";
        private const string EventEndRequest = "Microsoft.AspNetCore.Hosting.EndRequest";
        private const string EventHostingUnhandledException = "Microsoft.AspNetCore.Hosting.UnhandledException";
        private const string EventDiagnosticsHandledException = "Microsoft.AspNetCore.Diagnostics.HandledException";
        private const string EventDiagnosticsUnhandledException = "Microsoft.AspNetCore.Diagnostics.UnhandledException";

        private const string Component = "AspNetCore.Request";

        private const string ItemsKey = "ot-RequestScope";

        public RequestInterceptor(ILoggerFactory loggerFactory, ITracer tracer)
            : base(loggerFactory, tracer)
        {
        }

        protected override bool IsEnabled(string listenerName)
        {
            if (listenerName == EventBeginRequest)
                return true;
            if (listenerName == EventEndRequest)
                return true;
            if (listenerName == EventHostingUnhandledException)
                return true;
            if (listenerName == EventDiagnosticsHandledException)
                return true;
            if (listenerName == EventDiagnosticsUnhandledException)
                return true;

            return false;
        }

        [DiagnosticName(EventBeginRequest)]
        public void OnBeginRequest(HttpContext httpContext)
        {
            Execute(() =>
            {
                var request = httpContext.Request;

                var extractedSpanContext = TryExtractSpanContext(request);

                var operationName = GetOperationName(request);

                var scope = Tracer.BuildSpan(operationName)
                    .AsChildOf(extractedSpanContext)
                    .WithTag(Tags.Component.Key, Component)
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindServer)
                    .WithTag(Tags.HttpMethod.Key, request.Method)
                    .WithTag(Tags.HttpUrl.Key, request.GetDisplayUrl())
                    .StartActive(finishSpanOnDispose: true);

                // We don't rely on the ScopeManager as someone could have messed up with the disposal-chain in ScopeManager.
                httpContext.Items[ItemsKey] = scope;
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
        [DiagnosticName(EventHostingUnhandledException)]
        public void OnHostingUnhandledException(HttpContext httpContext, Exception exception)
        {
            ExecuteOnScope(httpContext, scope =>
            {
                scope.Span.SetException(exception);
                Tags.HttpStatus.Set(scope.Span, httpContext.Response.StatusCode);
                scope.Dispose();
            });
        }

        [DiagnosticName(EventDiagnosticsHandledException)]
        public void OnDiagnosticsHandledException(HttpContext httpContext, Exception exception)
        {
            ExecuteOnScope(httpContext, scope =>
            {
                scope.Span.SetException(exception);
            });
        }

        [DiagnosticName(EventDiagnosticsUnhandledException)]
        public void OnDiagnosticsUnhandledException(HttpContext httpContext, Exception exception)
        {
            ExecuteOnScope(httpContext, scope =>
            {
                scope.Span.SetException(exception);
            });
        }

        private ISpanContext TryExtractSpanContext(HttpRequest request)
        {
            try
            {
                ISpanContext spanContext = Tracer.Extract(BuiltinFormats.HttpHeaders, new HeaderDictionaryCarrier(request.Headers));
                return spanContext;
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "Extracting SpanContext failed");
                return null;
            }
        }

        private string GetOperationName(HttpRequest request)
        {
            // TODO @cweiss Make this configurable.
            return request.Path;
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
