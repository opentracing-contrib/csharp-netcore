using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.AspNetCore.Interceptors.Mvc
{
    internal sealed class MvcInterceptor : DiagnosticInterceptor
    {
        // Events
        private const string EventBeforeAction = "Microsoft.AspNetCore.Mvc.BeforeAction";
        private const string EventAfterAction = "Microsoft.AspNetCore.Mvc.AfterAction";
        private const string EventBeforeActionResult = "Microsoft.AspNetCore.Mvc.BeforeActionResult";
        private const string EventAfterActionResult = "Microsoft.AspNetCore.Mvc.AfterActionResult";

        private const string ActionComponent = "AspNetCore.MvcAction";
        private const string ActionTagActionName = "action";
        private const string ActionTagControllerName = "controller";

        private const string ResultComponent = "AspNetCore.MvcResult";
        private const string ResultTagType = "result.type";

        private readonly ProxyAdapter _proxyAdapter;

        protected override string ListenerName => "Microsoft.AspNetCore";

        public MvcInterceptor(ILoggerFactory loggerFactory, ITracer tracer)
            : base(loggerFactory, tracer)
        {
            _proxyAdapter = new ProxyAdapter();

            _proxyAdapter.Register("Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor");
            _proxyAdapter.Register("Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor");
        }

        [DiagnosticName(EventBeforeAction)]
        public void OnBeforeAction(object actionDescriptor, HttpContext httpContext)
        {
            // NOTE: This event is the start of the action pipeline. The action has been selected, the route
            //       has been selected but no filters have run and model binding hasn't occured.
            Execute(() =>
            {
                IActionDescriptor typedActionDescriptor = ConvertActionDescriptor(actionDescriptor);

                string operationName = $"action_{typedActionDescriptor.ControllerName}/{typedActionDescriptor.ActionName}";

                Tracer.BuildSpan(operationName)
                    .WithTag(Tags.Component.Key, ActionComponent)
                    .WithTag(ActionTagControllerName, typedActionDescriptor.ControllerName)
                    .WithTag(ActionTagActionName, typedActionDescriptor.ActionName)
                    .StartActive(finishSpanOnDispose: true);
            });
        }

        [DiagnosticName(EventAfterAction)]
        public void OnAfterAction(HttpContext httpContext)
        {
            DisposeActiveScope();
        }

        [DiagnosticName(EventBeforeActionResult)]
        public void OnBeforeActionResult(IActionContext actionContext, object result)
        {
            // NOTE: This event is the start of the result pipeline. The action has been executed, but
            //       we haven't yet determined which view (if any) will handle the request

            Execute(() =>
            {
                string resultType = result.GetType().Name;
                string operationName = $"result_{resultType}";

                Tracer.BuildSpan(operationName)
                    .WithTag(Tags.Component.Key, ResultComponent)
                    .WithTag(ResultTagType, resultType)
                    .StartActive(finishSpanOnDispose: true);
            });
        }

        [DiagnosticName(EventAfterActionResult)]
        public void OnAfterActionResult(IActionContext actionContext)
        {
            DisposeActiveScope();
        }

        private IActionDescriptor ConvertActionDescriptor(object actionDescriptor)
        {
            IActionDescriptor typedActionDescriptor = null;

            // NOTE: ActionDescriptor is usually ControllerActionDescriptor but the compile time type is
            //       ActionDescriptor. This is a problem because we are missing the ControllerName which
            //       we use a lot.
            switch (actionDescriptor.GetType().FullName)
            {
                case "Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor":
                    typedActionDescriptor = _proxyAdapter.Process<IActionDescriptor>("Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor", actionDescriptor);
                    break;
                case "Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor":
                    typedActionDescriptor = _proxyAdapter.Process<IActionDescriptor>("Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor", actionDescriptor);
                    break;
            }

            return typedActionDescriptor;
        }
    }
}
