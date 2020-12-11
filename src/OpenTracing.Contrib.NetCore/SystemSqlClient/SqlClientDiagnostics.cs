using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.SystemSqlClient
{
    internal sealed class SqlClientDiagnostics : DiagnosticEventObserver
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        private static readonly PropertyFetcher _writeCommandBefore_CommandFetcher = new PropertyFetcher("Command");
        private static readonly PropertyFetcher _writeCommandError_CommandFetcher = new PropertyFetcher("Command");
        private static readonly PropertyFetcher _writeCommandAfter_CommandFetcher = new PropertyFetcher("Command");
        private static readonly PropertyFetcher _exception_ExceptionFetcher = new PropertyFetcher("Exception");

        private readonly SqlClientDiagnosticOptions _options;
        private readonly ConcurrentDictionary<object, ISpan> _spanStorage;

        public SqlClientDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<SqlClientDiagnosticOptions> options)
           : base(loggerFactory, tracer, options?.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _spanStorage = new ConcurrentDictionary<object, ISpan>();
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        /// <summary>
        /// Both diagnostic listeners for System.Data.SqlClient and Microsoft.Data.SqlClient use the same listener name, 
        /// so we need to make sure this observer gets the correct events.
        /// </summary>
        protected override bool IsSupportedEvent(string eventName) => eventName.StartsWith("System.");

        protected override IEnumerable<string> HandledEventNames()
        {
            yield return "System.Data.SqlClient.WriteCommandBefore";
            yield return "System.Data.SqlClient.WriteCommandError";
            yield return "System.Data.SqlClient.WriteCommandAfter";
        }

        protected override void HandleEvent(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "System.Data.SqlClient.WriteCommandBefore":
                    {
                        var cmd = (SqlCommand)_writeCommandBefore_CommandFetcher.Fetch(untypedArg);

                        var activeSpan = Tracer.ActiveSpan;

                        if (activeSpan == null && !_options.StartRootSpans)
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring SQL command due to missing parent span");
                            }
                            return;
                        }

                        if (IgnoreEvent(cmd))
                        {
                            if (IsLogLevelTraceEnabled)
                            {
                                Logger.LogTrace("Ignoring SQL command due to IgnorePatterns");
                            }
                            return;
                        }

                        string operationName = _options.OperationNameResolver(cmd);

                        var span = Tracer.BuildSpan(operationName)
                            .AsChildOf(activeSpan)
                            .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                            .WithTag(Tags.Component, _options.ComponentName)
                            .WithTag(Tags.DbInstance, cmd.Connection.Database)
                            .WithTag(Tags.DbStatement, cmd.CommandText)
                            .Start();

                        _spanStorage.TryAdd(cmd, span);
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandError":
                    {
                        var cmd = (SqlCommand)_writeCommandError_CommandFetcher.Fetch(untypedArg);
                        var ex = (Exception)_exception_ExceptionFetcher.Fetch(untypedArg);

                        if (_spanStorage.TryRemove(cmd, out var span))
                        {
                            span.SetException(ex);
                            span.Finish();
                        }
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandAfter":
                    {
                        var cmd = (SqlCommand)_writeCommandAfter_CommandFetcher.Fetch(untypedArg);

                        if (_spanStorage.TryRemove(cmd, out var span))
                        {
                            span.Finish();
                        }
                    }
                    break;

                default:
                    HandleUnknownEvent(eventName, untypedArg);
                    break;
            }
        }

        private bool IgnoreEvent(SqlCommand sqlCommand)
        {
            foreach (Func<SqlCommand, bool> ignore in _options.IgnorePatterns)
            {
                if (ignore(sqlCommand))
                    return true;
            }

            return false;
        }
    }
}
