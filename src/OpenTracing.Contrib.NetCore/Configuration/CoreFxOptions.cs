using OpenTracing.Contrib.NetCore.DiagnosticSubscribers.CoreFx;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public sealed class CoreFxOptions
    {
        public GenericDiagnosticOptions GenericDiagnostic { get; } = new GenericDiagnosticOptions();

        public HttpHandlerDiagnosticOptions HttpHandlerDiagnostic { get; } = new HttpHandlerDiagnosticOptions();
    }
}
