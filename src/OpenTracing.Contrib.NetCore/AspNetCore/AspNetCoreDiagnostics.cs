using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    /// <summary>
    /// Instruments ASP.NET Core.
    /// <para/>
    /// Unfortunately, ASP.NET Core only uses one <see cref="System.Diagnostics.DiagnosticListener"/> instance
    /// for everything so we also only create one observer to ensure best performance.
    /// <para/>Hosting events: https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/Internal/HostingApplicationDiagnostics.cs
    /// </summary>
    internal sealed class AspNetCoreDiagnostics : DiagnosticEventObserver
    {
        public const string DiagnosticListenerName = "Microsoft.AspNetCore";

        private const string HostingScopeItemsKey = "ot-HttpRequestIn";
        private const string ActionScopeItemsKey = "ot-MvcAction";
        private const string ActionResultScopeItemsKey = "ot-MvcActionResult";

        private const string ActionComponent = "AspNetCore.MvcAction";
        private const string ActionTagActionName = "action";
        private const string ActionTagControllerName = "controller";

        private const string ResultComponent = "AspNetCore.MvcResult";
        private const string ResultTagType = "result.type";

        private static readonly PropertyFetcher _httpRequestIn_start_HttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher _httpRequestIn_stop_HttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher _unhandledException_HttpContextFetcher = new PropertyFetcher("httpContext");
        private static readonly PropertyFetcher _unhandledException_ExceptionFetcher = new PropertyFetcher("exception");
        private static readonly PropertyFetcher _beforeAction_httpContextFetcher = new PropertyFetcher("httpContext");
        private static readonly PropertyFetcher _beforeAction_ActionDescriptorFetcher = new PropertyFetcher("actionDescriptor");
        private static readonly PropertyFetcher _afterAction_httpContextFetcher = new PropertyFetcher("httpContext");
        private static readonly PropertyFetcher _beforeActionResult_actionContextFetcher = new PropertyFetcher("actionContext");
        private static readonly PropertyFetcher _beforeActionResult_ResultFetcher = new PropertyFetcher("result");
        private static readonly PropertyFetcher _afterActionResult_actionContextFetcher = new PropertyFetcher("actionContext");

        internal static readonly string NoHostSpecified = string.Empty;

        private readonly AspNetCoreDiagnosticOptions _options;

        public AspNetCoreDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<AspNetCoreDiagnosticOptions> options)
            : base(loggerFactory, tracer, options?.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        protected override bool IsSupportedEvent(string eventName)
        {
            return eventName switch
            {
                // We don't want to get the old deprecated Hosting events.
                "Microsoft.AspNetCore.Hosting.BeginRequest" => false,
                "Microsoft.AspNetCore.Hosting.EndRequest" => false,
                _ => true,
            };
        }

        protected override IEnumerable<string> HandledEventNames()
        {
            yield return "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start";
            yield return "Microsoft.AspNetCore.Hosting.UnhandledException";
            yield return "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";
            yield return "Microsoft.AspNetCore.Mvc.BeforeAction";
            yield return "Microsoft.AspNetCore.Mvc.AfterAction";
            yield return "Microsoft.AspNetCore.Mvc.BeforeActionResult";
            yield return "Microsoft.AspNetCore.Mvc.AfterActionResult";
        }

        protected override void HandleEvent(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    {
                        var httpContext = (HttpContext)_httpRequestIn_start_HttpContextFetcher.Fetch(untypedArg);

                        if (Tracer.ActiveSpan == null && !_options.StartRootSpans)
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring request due to missing parent span");
                            }
                            return;
                        }

                        if (ShouldIgnore(httpContext))
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring request");
                            }
                        }
                        else
                        {
                            var request = httpContext.Request;

                            ISpanContext extractedSpanContext = null;

                            if (_options.Hosting.ExtractEnabled?.Invoke(httpContext) ?? true)
                            {
                                extractedSpanContext = Tracer.Extract(BuiltinFormats.HttpHeaders, new RequestHeadersExtractAdapter(request.Headers));
                            }

                            string operationName = _options.Hosting.OperationNameResolver(httpContext);

                            IScope scope = Tracer.BuildSpan(operationName)
                                .AsChildOf(extractedSpanContext)
                                .WithTag(Tags.Component, _options.Hosting.ComponentName)
                                .WithTag(Tags.SpanKind, Tags.SpanKindServer)
                                .WithTag(Tags.HttpMethod, request.Method)
                                .WithTag(Tags.HttpUrl, GetDisplayUrl(request))
                                .StartActive();

                            _options.Hosting.OnRequest?.Invoke(scope.Span, httpContext);

                            httpContext.Items[HostingScopeItemsKey] = scope;
                        }
                    }
                    break;

                case "Microsoft.AspNetCore.Hosting.UnhandledException":
                    {
                        var httpContext = (HttpContext)_unhandledException_HttpContextFetcher.Fetch(untypedArg);
                        var exception = (Exception)_unhandledException_ExceptionFetcher.Fetch(untypedArg);

                        var scope = httpContext.Items[HostingScopeItemsKey] as IScope;
                        if (scope != null)
                        {
                            var span = scope.Span;

                            span.SetException(exception);

                            _options.Hosting.OnError?.Invoke(span, exception, httpContext);
                        }
                    }
                    break;

                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    {
                        var httpContext = (HttpContext)_httpRequestIn_stop_HttpContextFetcher.Fetch(untypedArg);

                        var scope = httpContext.Items[HostingScopeItemsKey] as IScope;
                        if (scope != null)
                        {
                            httpContext.Items.Remove(HostingScopeItemsKey);

                            scope.Span.SetTag(Tags.HttpStatus, httpContext.Response.StatusCode);
                            scope.Dispose();
                        }

                        httpContext.Items.Remove(ActionResultScopeItemsKey);
                        httpContext.Items.Remove(ActionScopeItemsKey);
                    }
                    break;

                case "Microsoft.AspNetCore.Mvc.BeforeAction":
                    {
                        var httpContext = (HttpContext)_beforeAction_httpContextFetcher.Fetch(untypedArg);

                        // We only create this span if the entire request should be traced.
                        var scope = httpContext.Items[HostingScopeItemsKey] as IScope;
                        if (scope != null)
                        {
                            // NOTE: This event is the start of the action pipeline. The action has been selected, the route
                            //       has been selected but no filters have run and model binding hasn't occured.

                            var actionDescriptor = (ActionDescriptor)_beforeAction_ActionDescriptorFetcher.Fetch(untypedArg);
                            var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;

                            string operationName = controllerActionDescriptor != null
                                ? $"Action {controllerActionDescriptor.ControllerTypeInfo.FullName}/{controllerActionDescriptor.ActionName}"
                                : $"Action {actionDescriptor.DisplayName}";

                            var actionScope = Tracer.BuildSpan(operationName)
                                .AsChildOf(scope.Span)
                                .WithTag(Tags.Component, ActionComponent)
                                .WithTag(ActionTagControllerName, controllerActionDescriptor?.ControllerTypeInfo.FullName)
                                .WithTag(ActionTagActionName, controllerActionDescriptor?.ActionName)
                                .StartActive();

                            httpContext.Items[ActionScopeItemsKey] = actionScope;
                        }
                    }
                    break;

                case "Microsoft.AspNetCore.Mvc.AfterAction":
                    {
                        var httpContext = (HttpContext)_afterAction_httpContextFetcher.Fetch(untypedArg);
                        var scope = httpContext.Items[ActionScopeItemsKey] as IScope;
                        if (scope != null)
                        {
                            httpContext.Items.Remove(ActionScopeItemsKey);
                            scope.Dispose();
                        } 
                    }
                    break;

                case "Microsoft.AspNetCore.Mvc.BeforeActionResult":
                    {
                        var httpContext = ((ActionContext)_beforeActionResult_actionContextFetcher.Fetch(untypedArg)).HttpContext;

                        // We only create this span if the entire request should be traced.
                        var scope = httpContext.Items[HostingScopeItemsKey] as IScope;
                        if (scope != null)
                        {
                            // NOTE: This event is the start of the result pipeline. The action has been executed, but
                            //       we haven't yet determined which view (if any) will handle the request

                            object result = _beforeActionResult_ResultFetcher.Fetch(untypedArg);

                            string resultType = result.GetType().Name;
                            string operationName = $"Result {resultType}";

                            var actionResultScope = Tracer.BuildSpan(operationName)
                                .AsChildOf(scope.Span)
                                .WithTag(Tags.Component, ResultComponent)
                                .WithTag(ResultTagType, resultType)
                                .StartActive();

                            httpContext.Items[ActionResultScopeItemsKey] = actionResultScope;
                        }
                    }
                    break;

                case "Microsoft.AspNetCore.Mvc.AfterActionResult":
                    {
                        var httpContext = ((ActionContext)_afterActionResult_actionContextFetcher.Fetch(untypedArg)).HttpContext;
                        var scope = httpContext.Items[ActionResultScopeItemsKey] as IScope;
                        if (scope != null)
                        {
                            httpContext.Items.Remove(ActionResultScopeItemsKey);
                            scope.Dispose();
                        }

                    }
                    break;

                default:
                    HandleUnknownEvent(eventName, untypedArg);
                    break;
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
            foreach (Func<HttpContext, bool> ignore in _options.Hosting.IgnorePatterns)
            {
                if (ignore(httpContext))
                    return true;
            }

            return false;
        }
    }
}
