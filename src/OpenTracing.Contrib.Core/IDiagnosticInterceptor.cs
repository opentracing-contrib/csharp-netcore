using System;

namespace OpenTracing.Contrib.Core
{
    public interface IDiagnosticInterceptor : IDisposable
    {
        void Start();
    }
}
