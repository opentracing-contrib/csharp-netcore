using OpenTracing.Contrib.NetCore.DiagnosticSubscribers.AspNetCore;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public class AspNetCoreOptions
    {
        public RequestDiagnosticOptions RequestDiagnostic { get; } = new RequestDiagnosticOptions();
    }
}
