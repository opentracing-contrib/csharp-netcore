using Microsoft.AspNetCore.Http;

namespace OpenTracing.Contrib.AspNetCore.Interceptors.Mvc
{
    public interface IActionContext
    {
        object ActionDescriptor { get; }
        HttpContext HttpContext { get; }
    }
}
