// From https://github.com/aspnet/Logging/blob/dev/src/Microsoft.Extensions.Logging.Testing/XunitLoggerFactoryExtensions.cs

using Microsoft.Extensions.DependencyInjection;
using OpenTracing.Contrib.NetCore.Tests.XunitLogging;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging
{
    public static class XunitLoggerFactoryExtensions
    {
        public static ILoggingBuilder AddXunit(this ILoggingBuilder builder, ITestOutputHelper output)
        {
            builder.Services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(output));
            return builder;
        }

        public static ILoggingBuilder AddXunit(this ILoggingBuilder builder, ITestOutputHelper output, LogLevel minLevel)
        {
            builder.Services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(output, minLevel));
            return builder;
        }

        public static ILoggerFactory AddXunit(this ILoggerFactory loggerFactory, ITestOutputHelper output)
        {
            loggerFactory.AddProvider(new XunitLoggerProvider(output));
            return loggerFactory;
        }

        public static ILoggerFactory AddXunit(this ILoggerFactory loggerFactory, ITestOutputHelper output, LogLevel minLevel)
        {
            loggerFactory.AddProvider(new XunitLoggerProvider(output, minLevel));
            return loggerFactory;
        }
    }
}
