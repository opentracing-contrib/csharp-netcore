using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace OpenTracing.Contrib.NetCore
{
    /// <summary>
    /// Starts and stops all OpenTracing instrumentation components.
    /// </summary>
    public class InstrumentationService : IHostedService
    {
        private readonly IEnumerable<DiagnosticInterceptor> _interceptors;

        private bool _started;

        public InstrumentationService(IEnumerable<DiagnosticInterceptor> interceptors)
        {
            _interceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_started)
            {
                foreach (DiagnosticInterceptor interceptor in _interceptors)
                {
                    interceptor.Start();
                }

                _started = true;
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_started)
            {
                foreach (var interceptor in _interceptors)
                {
                    interceptor.Stop();
                }

                _started = false;
            }

            return Task.CompletedTask;
        }
    }
}
