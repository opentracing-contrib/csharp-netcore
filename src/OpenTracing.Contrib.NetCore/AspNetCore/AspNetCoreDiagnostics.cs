using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    /// <summary>
    /// Instruments ASP.NET Core.
    /// <para/>
    /// Unfortunately, ASP.NET Core only uses one <see cref="System.Diagnostics.DiagnosticListener"/> instance
    /// for everything so we also only create one observer to ensure best performance.
    /// <para/>Hosting events: https://github.com/aspnet/Hosting/blob/dev/src/Microsoft.AspNetCore.Hosting/Internal/HostingApplicationDiagnostics.cs
    /// </summary>
    internal sealed class AspNetCoreDiagnostics : DiagnosticListenerObserver
    {
        public const string DiagnosticListenerName = "Microsoft.AspNetCore";

        private readonly HostingEventProcessor _hostingEventProcessor;
        private readonly MvcEventProcessor _mvcEventProcessor;

        protected override string GetListenerName() => DiagnosticListenerName;

        public AspNetCoreDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<AspNetCoreDiagnosticOptions> options)
            : base(loggerFactory, tracer)
        {
            if (options?.Value == null)
                throw new ArgumentNullException(nameof(options));

            _hostingEventProcessor = new HostingEventProcessor(Tracer, Logger, options.Value.Hosting);
            _mvcEventProcessor = new MvcEventProcessor(Tracer, Logger, options.Value.Hosting.IgnorePatterns);
        }

        protected override bool IsEnabled(string eventName)
        {
            switch (eventName)
            {
                // We don't want to get the old deprecated Hosting events.
                case "Microsoft.AspNetCore.Hosting.BeginRequest": return false;
                case "Microsoft.AspNetCore.Hosting.EndRequest": return false;
                default: return true;
            }
        }

        protected override void OnNext(string eventName, object untypedArg)
        {
            if (!IsEnabled(eventName))
                return;

            bool eventProcessed = _hostingEventProcessor.ProcessEvent(eventName, untypedArg)
                || _mvcEventProcessor.ProcessEvent(eventName, untypedArg);

            if (!eventProcessed)
            {
                ProcessUnhandledEvent(eventName, untypedArg);
            }
        }
    }
}
