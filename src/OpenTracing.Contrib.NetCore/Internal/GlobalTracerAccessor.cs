using OpenTracing.Util;

namespace OpenTracing.Contrib.NetCore.Internal
{
    public class GlobalTracerAccessor : IGlobalTracerAccessor
    {
        public ITracer GetGlobalTracer()
        {
            return GlobalTracer.Instance;
        }
    }
}
