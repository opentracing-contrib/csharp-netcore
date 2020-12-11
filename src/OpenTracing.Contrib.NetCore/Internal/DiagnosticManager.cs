using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenTracing.Contrib.NetCore.Internal
{
    /// <summary>
    /// Subscribes to <see cref="DiagnosticListener.AllListeners"/> and forwards events to individual <see cref="DiagnosticObserver"/> instances.
    /// </summary>
    internal sealed class DiagnosticManager : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ITracer _tracer;
        private readonly IEnumerable<DiagnosticObserver> _diagnosticSubscribers;
        private readonly DiagnosticManagerOptions _options;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private IDisposable _allListenersSubscription;

        public bool IsRunning => _allListenersSubscription != null;

        public DiagnosticManager(
            ILoggerFactory loggerFactory,
            ITracer tracer,
            IEnumerable<DiagnosticObserver> diagnosticSubscribers,
            IOptions<DiagnosticManagerOptions> options)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (diagnosticSubscribers == null)
                throw new ArgumentNullException(nameof(diagnosticSubscribers));

            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _logger = loggerFactory.CreateLogger<DiagnosticManager>();

            _diagnosticSubscribers = diagnosticSubscribers;
        }

        public void Start()
        {
            if (_allListenersSubscription == null)
            {
                if (_tracer.IsNoopTracer() && !_options.StartInstrumentationForNoopTracer)
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

        void IObserver<DiagnosticListener>.OnCompleted()
        {
        }

        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener listener)
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
