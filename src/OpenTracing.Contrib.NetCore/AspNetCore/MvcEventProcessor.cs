using System;
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

        private static readonly PropertyFetcher _beforeAction_ActionDescriptorFetcher = new PropertyFetcher("actionDescriptor");
        private static readonly PropertyFetcher _beforeActionResult_ResultFetcher = new PropertyFetcher("result");

        private readonly ITracer _tracer;
        private readonly ILogger _logger;
        private readonly MvcDiagnosticOptions _options;

        public MvcEventProcessor(ITracer tracer, ILogger logger, MvcDiagnosticOptions options)
        {
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public bool ProcessEvent(string eventName, object arg)
        {
            switch (eventName)
            {
                case "Microsoft.AspNetCore.Mvc.BeforeAction":
                    {
                        var activeSpan = _tracer.ActiveSpan;
                        if (activeSpan == null && !_options.StartRootSpans)
                        {
                            _logger.LogDebug("Ignoring event (StartRootSpans=false)");
                            // We return true because the event should also not be processed by the GenericEventProcessor.
                            return true;
                        }

                        // NOTE: This event is the start of the action pipeline. The action has been selected, the route
                        //       has been selected but no filters have run and model binding hasn't occurred.

                        var actionDescriptor = (ActionDescriptor)_beforeAction_ActionDescriptorFetcher.Fetch(arg);
                        var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;

                        string operationName = controllerActionDescriptor != null
                            ? $"Action {controllerActionDescriptor.ControllerTypeInfo.FullName}/{controllerActionDescriptor.ActionName}"
                            : $"Action {actionDescriptor.DisplayName}";

                        _tracer.BuildSpan(operationName)
                            .AsChildOf(activeSpan)
                            .WithTag(Tags.Component, ActionComponent)
                            .WithTag(ActionTagControllerName, controllerActionDescriptor?.ControllerTypeInfo.FullName)
                            .WithTag(ActionTagActionName, controllerActionDescriptor?.ActionName)
                            .StartActive();
                    }
                    return true;

                case "Microsoft.AspNetCore.Mvc.AfterAction":
                    {
                        _tracer.ScopeManager.Active?.Dispose();
                    }
                    return true;

                case "Microsoft.AspNetCore.Mvc.BeforeActionResult":
                    {
                        var activeSpan = _tracer.ActiveSpan;
                        if (activeSpan == null && !_options.StartRootSpans)
                        {
                            _logger.LogDebug("Ignoring event (StartRootSpans=false)");
                            // We return true because the event should also not be processed by the GenericEventProcessor.
                            return true;
                        }

                        // NOTE: This event is the start of the result pipeline. The action has been executed, but
                        //       we haven't yet determined which view (if any) will handle the request

                        object result = _beforeActionResult_ResultFetcher.Fetch(arg);

                        string resultType = result.GetType().Name;
                        string operationName = $"Result {resultType}";

                        _tracer.BuildSpan(operationName)
                            .AsChildOf(activeSpan)
                            .WithTag(Tags.Component, ResultComponent)
                            .WithTag(ResultTagType, resultType)
                            .StartActive();
                    }
                    return true;

                case "Microsoft.AspNetCore.Mvc.AfterActionResult":
                    {
                        _tracer.ScopeManager.Active?.Dispose();
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
