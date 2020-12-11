using System.Collections.Generic;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public sealed class GenericDiagnosticOptions
    {
        public HashSet<string> IgnoredListenerNames { get; } = new HashSet<string>();

        public Dictionary<string, HashSet<string>> IgnoredEvents { get; } = new Dictionary<string, HashSet<string>>();
    }
}
