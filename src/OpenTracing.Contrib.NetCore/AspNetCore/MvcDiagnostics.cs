using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    internal sealed class MvcDiagnostics : DiagnosticSubscriberWithObserver
    {
        public const string DiagnosticListenerName = "Microsoft.AspNetCore";

        public const string EventBeforeAction = "Microsoft.AspNetCore.Mvc.BeforeAction";
        public const string EventAfterAction = "Microsoft.AspNetCore.Mvc.AfterAction";
        public const string EventBeforeActionResult = "Microsoft.AspNetCore.Mvc.BeforeActionResult";
        public const string EventAfterActionResult = "Microsoft.AspNetCore.Mvc.AfterActionResult";

        public static readonly Action<GenericDiagnosticOptions> GenericDiagnosticsExclusions = options =>
        {
            options.IgnoreEvent(DiagnosticListenerName, EventBeforeAction);
            options.IgnoreEvent(DiagnosticListenerName, EventAfterAction);
            options.IgnoreEvent(DiagnosticListenerName, EventBeforeActionResult);
            options.IgnoreEvent(DiagnosticListenerName, EventAfterActionResult);
        };

        private const string ActionComponent = "AspNetCore.MvcAction";
        private const string ActionTagActionName = "action";
        private const string ActionTagControllerName = "controller";

        private const string ResultComponent = "AspNetCore.MvcResult";
        private const string ResultTagType = "result.type";

        private readonly PropertyFetcher _beforeAction_ActionDescriptorFetcher = new PropertyFetcher("actionDescriptor");
        private readonly PropertyFetcher _beforeActionResult_ResultFetcher = new PropertyFetcher("result");

        protected override string ListenerName => DiagnosticListenerName;

        public MvcDiagnostics(ILoggerFactory loggerFactory, ITracer tracer)
            : base(loggerFactory, tracer)
        {
        }

        protected override void OnNextCore(string eventName, object arg)
        {
            switch (eventName)
            {
                case EventBeforeAction:
                    {
                        // NOTE: This event is the start of the action pipeline. The action has been selected, the route
                        //       has been selected but no filters have run and model binding hasn't occured.

                        var actionDescriptor = (ActionDescriptor)_beforeAction_ActionDescriptorFetcher.Fetch(arg);
                        var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;

                        string operationName = controllerActionDescriptor != null
                            ? $"Action {controllerActionDescriptor.ControllerTypeInfo.FullName}/{controllerActionDescriptor.ActionName}"
                            : $"Action {actionDescriptor.DisplayName}";

                        Tracer.BuildSpan(operationName)
                            .WithTag(Tags.Component.Key, ActionComponent)
                            .WithTag(ActionTagControllerName, controllerActionDescriptor?.ControllerTypeInfo.FullName)
                            .WithTag(ActionTagActionName, controllerActionDescriptor?.ActionName)
                            .StartActive(finishSpanOnDispose: true);
                    }
                    break;

                case EventAfterAction:
                    {
                        DisposeActiveScope(isScopeRequired: true);
                    }
                    break;

                case EventBeforeActionResult:
                    {
                        // NOTE: This event is the start of the result pipeline. The action has been executed, but
                        //       we haven't yet determined which view (if any) will handle the request

                        object result = _beforeActionResult_ResultFetcher.Fetch(arg);

                        string resultType = result.GetType().Name;
                        string operationName = $"Result {resultType}";

                        Tracer.BuildSpan(operationName)
                            .WithTag(Tags.Component.Key, ResultComponent)
                            .WithTag(ResultTagType, resultType)
                            .StartActive(finishSpanOnDispose: true);
                    }
                    break;

                case EventAfterActionResult:
                    {
                        DisposeActiveScope(isScopeRequired: true);
                    }
                    break;
            }
        }
    }
}
