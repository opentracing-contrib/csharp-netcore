using System;
using System.Data.Common;
using Microsoft.Extensions.DiagnosticAdapter;
using Microsoft.Extensions.Logging;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.DiagnosticSubscribers.EntityFrameworkCore
{
    internal sealed class EFCoreDiagnosticSubscriber : DiagnosticSubscriberWithAdapter
    {
        private const string Component = "EFCore";

        private const string TagCommandText = "ef.command";
        private const string TagMethod = "ef.method";
        private const string TagIsAsync = "ef.async";

        // https://github.com/aspnet/EntityFrameworkCore/blob/dev/src/EFCore/DbLoggerCategory.cs
        protected override string ListenerName => "Microsoft.EntityFrameworkCore";

        public EFCoreDiagnosticSubscriber(ILoggerFactory loggerFactory, ITracer tracer)
            : base(loggerFactory, tracer)
        {
        }

        [DiagnosticName("Database.Command.CommandExecuting")]
        public void OnCommandExecuting(DbCommand command, string executeMethod, bool isAsync)
        {
            Execute(() =>
            {
                // TODO @cweiss !! OperationName ??
                string operationName = executeMethod;

                Tracer.BuildSpan(operationName)
                    .WithTag(Tags.SpanKind.Key, Tags.SpanKindClient)
                    .WithTag(Tags.Component.Key, Component)
                    .WithTag(TagCommandText, command.CommandText)
                    .WithTag(TagMethod, executeMethod)
                    .WithTag(TagIsAsync, isAsync)
                    .StartActive(finishSpanOnDispose: true);
            });
        }

        [DiagnosticName("Database.Command.CommandExecuted")]
        public void OnAfterExecuteCommand(DbCommand command, string executeMethod, bool isAsync)
        {
            DisposeActiveScope();
        }

        [DiagnosticName("Database.Command.CommandError")]
        public void OnCommandError(DbCommand command, string executeMethod, bool isAsync, Exception exception)
        {
            Execute(() =>
            {
                var scope = Tracer.ScopeManager.Active;
                if (scope == null)
                {
                    Logger.LogError("Span not found");
                    return;
                }

                scope.Span.SetException(exception);
                scope.Dispose();
            });
        }
    }
}
