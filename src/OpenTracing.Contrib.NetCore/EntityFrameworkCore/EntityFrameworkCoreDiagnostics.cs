using System;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.EntityFrameworkCore
{
    internal sealed class EntityFrameworkCoreDiagnostics : DiagnosticSubscriberWithObserver
    {
        // https://github.com/aspnet/EntityFrameworkCore/blob/dev/src/EFCore/DbLoggerCategory.cs
        public const string DiagnosticListenerName = "Microsoft.EntityFrameworkCore";

        public const string EventOnCommandExecuting = DiagnosticListenerName + ".Database.Command.CommandExecuting";
        public const string EventOnCommandExecuted = DiagnosticListenerName + ".Database.Command.CommandExecuted";
        public const string EventOnCommandError = DiagnosticListenerName + ".Database.Command.CommandError";

        public static readonly Action<GenericDiagnosticOptions> GenericDiagnosticsExclusions = options =>
        {
            options.IgnoreEvent(DiagnosticListenerName, EventOnCommandExecuting);
            options.IgnoreEvent(DiagnosticListenerName, EventOnCommandExecuted);
            options.IgnoreEvent(DiagnosticListenerName, EventOnCommandError);
        };

        private const string TagMethod = "db.method";
        private const string TagIsAsync = "db.async";

        private readonly EntityFrameworkCoreOptions _options;

        protected override string ListenerName => DiagnosticListenerName;

        public EntityFrameworkCoreDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<EntityFrameworkCoreOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public override bool IsSubscriberEnabled()
        {
            return true;
        }

        protected override void OnNextCore(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case EventOnCommandExecuting:
                    {
                        CommandEventData args = (CommandEventData)untypedArg;

                        string operationName = _options.OperationNameResolver(args);

                        Tracer.BuildSpan(operationName)
                            .WithTag(Tags.SpanKind.Key, Tags.SpanKindClient)
                            .WithTag(Tags.Component.Key, _options.ComponentName)
                            .WithTag(Tags.DbInstance.Key, args.Command.Connection.Database)
                            .WithTag(Tags.DbStatement.Key, args.Command.CommandText)
                            .WithTag(TagMethod, args.ExecuteMethod.ToString())
                            .WithTag(TagIsAsync, args.IsAsync)
                            .StartActive(finishSpanOnDispose: true);
                    }
                    break;

                case EventOnCommandExecuted:
                    {
                        DisposeActiveScope(isScopeRequired: true);
                    }
                    break;

                case EventOnCommandError:
                    {
                        CommandErrorEventData args = (CommandErrorEventData)untypedArg;

                        // The "CommandExecuted" event is NOT called in case of an exception,
                        // so we have to dispose the scope here as well!
                        DisposeActiveScope(isScopeRequired: true, exception: args.Exception);
                    }
                    break;
            }
        }
    }
}
