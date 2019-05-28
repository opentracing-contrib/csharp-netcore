using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    internal sealed class MvcEventProcessor
    {
        private const string ActionComponent = "AspNetCore.MvcAction";
        private const string ActionTagActionName = "action";
        private const string ActionTagControllerName = "controller";

        private const string ResultComponent = "AspNetCore.MvcResult";
        private const string ResultTagType = "result.type";

        private static readonly PropertyFetcher _beforeAction_httpContextFetcher = new PropertyFetcher("httpContext");
        private static readonly PropertyFetcher _beforeAction_ActionDescriptorFetcher = new PropertyFetcher("actionDescriptor");
        private static readonly PropertyFetcher _beforeActionResult_actionContextFetcher = new PropertyFetcher("actionContext");
        private static readonly PropertyFetcher _beforeActionResult_ResultFetcher = new PropertyFetcher("result");

        private readonly ITracer _tracer;
        private readonly ILogger _logger;
        private readonly IList<Func<HttpContext, bool>> _ignorePatterns;

        public MvcEventProcessor(ITracer tracer, ILogger logger, IList<Func<HttpContext, bool>> ignorePatterns)
        {
            _ignorePatterns = ignorePatterns;
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool ProcessEvent(string eventName, object arg)
        {
            switch (eventName)
            {
                case "Microsoft.AspNetCore.Mvc.BeforeAction":
                {
                    var httpContext = (HttpContext)_beforeAction_httpContextFetcher.Fetch(arg);

                    if (ShouldIgnore(httpContext))
                    {
                        _logger.LogDebug("Ignoring request");
                    }
                    else
                    {
                        // NOTE: This event is the start of the action pipeline. The action has been selected, the route
                        //       has been selected but no filters have run and model binding hasn't occured.

                        var actionDescriptor = (ActionDescriptor)_beforeAction_ActionDescriptorFetcher.Fetch(arg);
                        var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;

                        string operationName = controllerActionDescriptor != null
                            ? $"Action {controllerActionDescriptor.ControllerTypeInfo.FullName}/{controllerActionDescriptor.ActionName}"
                            : $"Action {actionDescriptor.DisplayName}";

                        _tracer.BuildSpan(operationName)
                            .WithTag(Tags.Component, ActionComponent)
                            .WithTag(ActionTagControllerName, controllerActionDescriptor?.ControllerTypeInfo.FullName)
                            .WithTag(ActionTagActionName, controllerActionDescriptor?.ActionName)
                            .StartActive();
                    }
                }
                    return true;

                case "Microsoft.AspNetCore.Mvc.AfterAction":
                    {
                        _tracer.ScopeManager.Active?.Dispose();
                    }
                    return true;

                case "Microsoft.AspNetCore.Mvc.BeforeActionResult":
                {
                    var httpContext = ((ActionContext) _beforeActionResult_actionContextFetcher.Fetch(arg)).HttpContext;

                    if (ShouldIgnore(httpContext))
                    {
                        _logger.LogDebug("Ignoring request");
                    }
                    else
                    {
                        // NOTE: This event is the start of the result pipeline. The action has been executed, but
                        //       we haven't yet determined which view (if any) will handle the request

                        object result = _beforeActionResult_ResultFetcher.Fetch(arg);

                        string resultType = result.GetType().Name;
                        string operationName = $"Result {resultType}";

                        _tracer.BuildSpan(operationName)
                            .WithTag(Tags.Component, ResultComponent)
                            .WithTag(ResultTagType, resultType)
                            .StartActive();
                    }
                }
                    return true;

                case "Microsoft.AspNetCore.Mvc.AfterActionResult":
                    {
                        _tracer.ScopeManager.Active?.Dispose();
                    }
                    return true;

                default: return false;
            }
        }
        
        private bool ShouldIgnore(HttpContext httpContext)
        {
            return _ignorePatterns.Any(ignore => ignore(httpContext));
        }
    }
}
