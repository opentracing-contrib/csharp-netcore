namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    public class MvcDiagnosticOptions
    {
        /// <summary>
        /// Whether or not spans should be created if there is no currently active span.
        /// This is disabled by default because there usually should be some parent component above MVC that starts spans (e.g. a HTTP request in Hosting) and if that parent component
        /// should not be traced, usually the entire request should not be traced.
        /// </summary>
        public bool StartRootSpans { get; set; } = false;
    }
}
