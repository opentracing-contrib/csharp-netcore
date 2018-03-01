using System;

namespace OpenTracing.Contrib.Core
{
    public interface IInstrumentor : IDisposable
    {
        void Start();
    }
}
