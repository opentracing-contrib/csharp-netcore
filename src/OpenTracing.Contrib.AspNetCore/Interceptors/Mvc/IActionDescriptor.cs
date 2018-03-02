using System;
using System.Reflection;

namespace OpenTracing.Contrib.AspNetCore.Interceptors.Mvc
{
    public interface IActionDescriptor
    {
        string Id { get; }
        string DisplayName { get; }
        string ActionName { get; }
        string ControllerName { get; }
        Type ControllerTypeInfo { get; }
        MethodInfo MethodInfo { get; }
    }
}
