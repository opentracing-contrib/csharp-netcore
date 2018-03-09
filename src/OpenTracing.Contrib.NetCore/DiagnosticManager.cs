using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.DiagnosticSubscribers;

namespace OpenTracing.Contrib.NetCore
{
    public sealed class DiagnosticManager : IDisposable
    {
        private readonly ILogger<DiagnosticManager> _logger;
        private readonly IEnumerable<DiagnosticSubscriber> _diagnosticSubscribers;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private IDisposable _allListenersSubscription;

        public DiagnosticManager(ILoggerFactory loggerFactory, IEnumerable<DiagnosticSubscriber> diagnosticSubscribers)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (diagnosticSubscribers == null)
                throw new ArgumentNullException(nameof(diagnosticSubscribers));

            _logger = loggerFactory.CreateLogger<DiagnosticManager>();
            _diagnosticSubscribers = diagnosticSubscribers.Where(x => x.IsEnabled());
        }

        public void Start()
        {
            if (_allListenersSubscription == null)
            {
                _logger.LogTrace("Starting AllListeners subscription");

                _allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(listener =>
                {
                    foreach (var subscriber in _diagnosticSubscribers)
                    {
                        IDisposable subscription = subscriber.SubscribeIfMatch(listener);
                        if (subscription != null)
                        {
                            _logger.LogTrace($"Subscriber '{subscriber.GetType().Name}' returned subscription for '{listener.Name}'");
                            _subscriptions.Add(subscription);
                        }
                    }
                });
            }
        }

        public void Stop()
        {
            if (_allListenersSubscription != null)
            {
                _logger.LogTrace("Stopping AllListeners subscription");

                _allListenersSubscription.Dispose();
                _allListenersSubscription = null;

                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }

                _subscriptions.Clear();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
