using System.Collections.Generic;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public abstract class DiagnosticOptions
    {
        /// <summary>
        /// Defines whether or not generic events from this DiagnostSource should be logged as events.
        /// </summary>
        public bool LogEvents { get; set; } = true;

        /// <summary>
        /// Defines specific event names that should NOT be logged as events. Set <see cref="LogEvents"/> to `false` if you don't want any events to be logged.
        /// </summary>
        public HashSet<string> IgnoredEvents { get; } = new HashSet<string>();

        /// <summary>
        /// Defines whether or not a span should be created if there is no parent span.
        /// </summary>
        public bool StartRootSpans { get; set; } = true;
    }
}
