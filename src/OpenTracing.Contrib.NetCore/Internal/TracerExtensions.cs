using OpenTracing.Noop;
using OpenTracing.Util;

namespace OpenTracing.Contrib.NetCore.Internal
{
    internal static class TracerExtensions
    {
        public static bool IsNoopTracer(this ITracer tracer)
        {
            if (tracer is NoopTracer)
                return true;

            // There's no way to check the underlying tracer on the instance so we have to check the static method.
            if (tracer is GlobalTracer && !GlobalTracer.IsRegistered())
                return true;

            return false;
        }
    }
}
