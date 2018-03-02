using System;

namespace OpenTracing.Contrib.Core
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
