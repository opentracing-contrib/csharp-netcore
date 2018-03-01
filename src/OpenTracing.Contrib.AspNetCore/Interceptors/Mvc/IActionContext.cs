using Microsoft.AspNetCore.Http;

namespace OpenTracing.Contrib.AspNetCore.Interceptors.Mvc
{
    internal interface IActionContext
    {
        object ActionDescriptor { get; }
        HttpContext HttpContext { get; }
    }
}
