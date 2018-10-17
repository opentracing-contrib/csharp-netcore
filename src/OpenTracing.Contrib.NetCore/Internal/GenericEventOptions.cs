using System;
using System.Collections.Generic;

namespace OpenTracing.Contrib.NetCore.Internal
{
    public class GenericEventOptions
    {
        public bool IgnoreAll { get; set; }

        public HashSet<string> IgnoredListenerNames { get; } = new HashSet<string>();

        public Dictionary<string, HashSet<string>> IgnoredEvents { get; } = new Dictionary<string, HashSet<string>>();

        public void IgnoreListener(string listenerName)
        {
            if (listenerName == null)
                throw new ArgumentNullException(nameof(listenerName));

            IgnoredListenerNames.Add(listenerName);
        }

        public void IgnoreEvent(string listenerName, string eventName)
        {
            if (listenerName == null)
                throw new ArgumentNullException(nameof(listenerName));

            if (eventName == null)
                throw new ArgumentNullException(nameof(eventName));

            if (!IgnoredEvents.TryGetValue(listenerName, out var ignoredListenerEvents))
            {
                ignoredListenerEvents = new HashSet<string>();
                IgnoredEvents.Add(listenerName, ignoredListenerEvents);
            }

            ignoredListenerEvents.Add(eventName);
        }

        public bool IsIgnored(string listenerName)
        {
            if (IgnoreAll)
                return true;

            if (IgnoredListenerNames.Contains(listenerName))
                return true;

            return false;
        }

        public bool IsIgnored(string listenerName, string eventName)
        {
            if (IgnoreAll)
                return true;

            if (IgnoredListenerNames.Contains(listenerName))
                return true;

            if (IgnoredEvents.TryGetValue(listenerName, out var set) && set.Contains(eventName))
                return true;

            return false;
        }
    }
}
