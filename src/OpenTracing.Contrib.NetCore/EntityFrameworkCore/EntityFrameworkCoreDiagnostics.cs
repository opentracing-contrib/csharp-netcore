using System;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.EntityFrameworkCore
{
    internal sealed class EntityFrameworkCoreDiagnostics : DiagnosticListenerObserver
    {
        // https://github.com/aspnet/EntityFrameworkCore/blob/dev/src/EFCore/DbLoggerCategory.cs
        public const string DiagnosticListenerName = "Microsoft.EntityFrameworkCore";

        private const string TagMethod = "db.method";
        private const string TagIsAsync = "db.async";

        private readonly EntityFrameworkCoreDiagnosticOptions _options;

        protected override string GetListenerName() => DiagnosticListenerName;

        public EntityFrameworkCoreDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<EntityFrameworkCoreDiagnosticOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override void OnNext(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting":
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

                case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted":
                    {
                        DisposeActiveScope(isScopeRequired: true);
                    }
                    break;

                case "Microsoft.EntityFrameworkCore.Database.Command.CommandError":
                    {
                        CommandErrorEventData args = (CommandErrorEventData)untypedArg;

                        // The "CommandExecuted" event is NOT called in case of an exception,
                        // so we have to dispose the scope here as well!
                        DisposeActiveScope(isScopeRequired: true, exception: args.Exception);
                    }
                    break;

                default:
                    {
                        ProcessUnhandledEvent(eventName, untypedArg);
                    }
                    break;
            }
        }
    }
}
