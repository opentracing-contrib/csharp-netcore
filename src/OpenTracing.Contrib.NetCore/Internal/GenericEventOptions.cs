using System;
using System.Collections.Generic;

namespace OpenTracing.Contrib.NetCore.Internal
{
    public class GenericEventOptions
    {
        private bool _hasFilter;
        private bool _ignoreAll;
        private Dictionary<string, HashSet<string>> _ignoredEvents;
        private HashSet<string> _ignoredListeners;

        /// <summary>
        /// is ignore all events log
        /// </summary>
        public bool IgnoreAll
        {
            get => _ignoreAll;
            set
            {
                _ignoreAll = value;
                CheckHasFilter();
            }
        }

        /// <summary>
        /// listener names which will be ignored for logging
        /// </summary>
        public HashSet<string> IgnoredListenerNames
        {
            get => _ignoredListeners;
            set
            {
                _ignoredListeners = value;
                CheckHasFilter();
            }
        }

        /// <summary>
        /// events which will be ignored for logging. Key is listener name, HashSet item is event name of the listener
        /// </summary>
        public Dictionary<string, HashSet<string>> IgnoredEvents
        {
            get => _ignoredEvents;
            set
            {
                _ignoredEvents = value;
                CheckHasFilter();
            }
        }

        public void IgnoreListener(string listenerName)
        {
            if (listenerName == null)
                throw new ArgumentNullException(nameof(listenerName));

            if (IgnoredListenerNames == null)
                IgnoredListenerNames = new HashSet<string>();

            IgnoredListenerNames.Add(listenerName);
            _hasFilter = true;
        }

        public void IgnoreEvent(string listenerName, string eventName)
        {
            if (listenerName == null)
                throw new ArgumentNullException(nameof(listenerName));

            if (eventName == null)
                throw new ArgumentNullException(nameof(eventName));

            if (IgnoredListenerNames == null)
                IgnoredListenerNames = new HashSet<string>();

            if (!IgnoredEvents.TryGetValue(listenerName, out var ignoredListenerEvents))
            {
                ignoredListenerEvents = new HashSet<string>();
                IgnoredEvents.Add(listenerName, ignoredListenerEvents);
            }

            ignoredListenerEvents.Add(eventName);
            _hasFilter = true;
        }

        /// <summary>
        /// usually used in DiagnosticObserver
        /// </summary>
        /// <param name="listenerName"></param>
        /// <returns></returns>
        public bool IsIgnored(string listenerName)
        {
            if (!_hasFilter)
                return false;

            if (IgnoreAll)
                return true;

            return IgnoredListenerNames.Count > 0
                && IgnoredListenerNames.Contains(listenerName);
        }

        public bool IsIgnored(string listenerName, string eventName)
        {
            if (!_hasFilter)
                return false;

            if (IgnoreAll)
                return true;

            return IgnoredEvents.TryGetValue(listenerName, out var set)
                && set.Contains(eventName);
        }

        private void CheckHasFilter()
        {
            if (IgnoredListenerNames == null)
                IgnoredListenerNames = new HashSet<string>();

            if (IgnoredEvents == null)
                IgnoredEvents = new Dictionary<string, HashSet<string>>();

            _hasFilter = IgnoreAll
                || IgnoredListenerNames.Count > 0
                || IgnoredEvents.Count > 0;
        }
    }
}
