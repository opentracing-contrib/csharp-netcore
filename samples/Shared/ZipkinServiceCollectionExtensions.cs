using System;
using Microsoft.Extensions.Hosting;
using Shared;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ZipkinServiceCollectionExtensions
    {
        public static IServiceCollection AddZipkin(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IHostedService, ZipkinService>();

            return services;
        }
    }
}
