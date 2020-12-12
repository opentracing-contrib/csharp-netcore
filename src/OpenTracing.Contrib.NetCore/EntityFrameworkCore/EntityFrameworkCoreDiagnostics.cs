using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.EntityFrameworkCore
{
    internal sealed class EntityFrameworkCoreDiagnostics : DiagnosticEventObserver
    {
        // https://github.com/aspnet/EntityFrameworkCore/blob/dev/src/EFCore/DbLoggerCategory.cs
        public const string DiagnosticListenerName = "Microsoft.EntityFrameworkCore";

        private const string TagMethod = "db.method";
        private const string TagIsAsync = "db.async";

        private readonly EntityFrameworkCoreDiagnosticOptions _options;
        private readonly ConcurrentDictionary<object, IScope> _scopeStorage;

        public EntityFrameworkCoreDiagnostics(ILoggerFactory loggerFactory, ITracer tracer,
            IOptions<EntityFrameworkCoreDiagnosticOptions> options)
            : base(loggerFactory, tracer, options?.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _scopeStorage = new ConcurrentDictionary<object, IScope>();
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        protected override IEnumerable<string> HandledEventNames()
        {
            yield return "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";
            yield return "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";
            yield return "Microsoft.EntityFrameworkCore.Database.Command.CommandError";
        }

        protected override void HandleEvent(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting":
                    {
                        CommandEventData args = (CommandEventData)untypedArg;

                        var activeSpan = Tracer.ActiveSpan;

                        if (activeSpan == null && !_options.StartRootSpans)
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring EF command due to missing parent span");
                            }
                            return;
                        }

                        if (IgnoreEvent(args))
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring EF command due to IgnorePatterns");
                            }
                            return;
                        }

                        string operationName = _options.OperationNameResolver(args);

                        var scope = Tracer.BuildSpan(operationName)
                            .AsChildOf(activeSpan)
                            .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                            .WithTag(Tags.Component, _options.ComponentName)
                            .WithTag(Tags.DbInstance, args.Command.Connection.Database)
                            .WithTag(Tags.DbStatement, args.Command.CommandText)
                            .WithTag(TagMethod, args.ExecuteMethod.ToString())
                            .WithTag(TagIsAsync, args.IsAsync)
                            .StartActive();

                        _scopeStorage.TryAdd(args.CommandId, scope);
                    }
                    break;

                case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted":
                    {
                        CommandExecutedEventData args = (CommandExecutedEventData)untypedArg;

                        if (_scopeStorage.TryRemove(args.CommandId, out var scope))
                        {
                            scope.Dispose();
                        }
                    }
                    break;

                case "Microsoft.EntityFrameworkCore.Database.Command.CommandError":
                    {
                        CommandErrorEventData args = (CommandErrorEventData)untypedArg;

                        // The "CommandExecuted" event is NOT called in case of an exception,
                        // so we have to dispose the scope here as well!
                        if (_scopeStorage.TryRemove(args.CommandId, out var scope))
                        {
                            scope.Span.SetException(args.Exception);
                            scope.Dispose();
                        }
                    }
                    break;

                default:
                    {
                        Dictionary<string, string> tags = null;
                        if (untypedArg is EventData eventArgs)
                        {
                            tags = new Dictionary<string, string>
                            {
                                { "level", eventArgs.LogLevel.ToString() },
                            };
                        }

                        HandleUnknownEvent(eventName, untypedArg, tags);
                        break;
                    }
                    
            }
        }

        private bool IgnoreEvent(CommandEventData eventData)
        {
            foreach (Func<CommandEventData, bool> ignore in _options.IgnorePatterns)
            {
                if (ignore(eventData))
                    return true;
            }

            return false;
        }
    }
}
