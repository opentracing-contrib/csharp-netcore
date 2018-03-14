using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenTracing.Noop;
using OpenTracing.Util;

namespace OpenTracing.Contrib.NetCore.Internal
{
    internal sealed class DiagnosticManager : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ITracer _tracer;
        private readonly IEnumerable<DiagnosticSubscriber> _diagnosticSubscribers;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private IDisposable _allListenersSubscription;

        public bool IsRunning => _allListenersSubscription != null;

        public DiagnosticManager(ILoggerFactory loggerFactory, ITracer tracer, IEnumerable<DiagnosticSubscriber> diagnosticSubscribers)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (tracer == null)
                throw new ArgumentNullException(nameof(tracer));

            if (diagnosticSubscribers == null)
                throw new ArgumentNullException(nameof(diagnosticSubscribers));

            _logger = loggerFactory.CreateLogger<DiagnosticManager>();
            _tracer = tracer;
            _diagnosticSubscribers = diagnosticSubscribers.Where(x => x.IsSubscriberEnabled());
        }

        public void Start()
        {
            if (_allListenersSubscription == null)
            {
                if (_tracer.IsNoopTracer())
                {
                    _logger.LogWarning("Instrumentation has not been started because no tracer was registered.");
                }
                else
                {
                    _logger.LogTrace("Starting AllListeners subscription");
                    _allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
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
