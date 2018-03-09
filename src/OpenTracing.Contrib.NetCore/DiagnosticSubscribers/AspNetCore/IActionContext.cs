using Microsoft.AspNetCore.Http;

namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers.AspNetCore
{
    public interface IActionContext
    {
        object ActionDescriptor { get; }
        HttpContext HttpContext { get; }
    }
}
