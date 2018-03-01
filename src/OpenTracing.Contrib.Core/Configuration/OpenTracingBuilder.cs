using System;
using Microsoft.Extensions.DependencyInjection;

namespace OpenTracing.Contrib.Core.Configuration
{
    public class OpenTracingBuilder : IOpenTracingBuilder
    {
        public IServiceCollection Services { get; }

        public OpenTracingBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
    }
}
