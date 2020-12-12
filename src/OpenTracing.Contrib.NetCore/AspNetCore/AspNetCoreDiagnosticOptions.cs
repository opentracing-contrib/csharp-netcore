using OpenTracing.Contrib.NetCore.AspNetCore;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public class AspNetCoreDiagnosticOptions : DiagnosticOptions
    {
        public HostingOptions Hosting { get; } = new HostingOptions();

        public AspNetCoreDiagnosticOptions()
        {
            // We create separate spans for MVC actions & results so we don't need these additional events by default.
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuting");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnActionExecution");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnActionExecuting");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnActionExecuting");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeActionMethod");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeControllerActionMethod");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterControllerActionMethod");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterActionMethod");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnActionExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnActionExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnActionExecution");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnActionExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnActionExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnActionExecution");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnResultExecuting");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnResultExecuting");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnResultExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnResultExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.BeforeOnResourceExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnResourceExecuted");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.AfterOnResourceExecuting");

            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.Razor.BeginInstrumentationContext");
            IgnoredEvents.Add("Microsoft.AspNetCore.Mvc.Razor.EndInstrumentationContext");
        }
    }
}
