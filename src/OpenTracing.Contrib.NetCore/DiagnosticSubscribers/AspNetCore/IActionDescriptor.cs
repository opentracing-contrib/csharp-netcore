using System;
using System.Reflection;

namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers.AspNetCore
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
