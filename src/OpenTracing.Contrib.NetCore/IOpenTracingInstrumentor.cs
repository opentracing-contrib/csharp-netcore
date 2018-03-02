using System;

namespace OpenTracing.Contrib.NetCore
{
    /// <summary>
    /// Responsible for starting and stopping all registered OpenTracing instrumentation components.
    /// </summary>
    public interface IOpenTracingInstrumentor : IDisposable
    {
        /// <summary>
        /// Starts the instrumentation.
        /// </summary>
        void Start();
    }
}
