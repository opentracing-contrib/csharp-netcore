using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.MicrosoftSqlClient
{
    internal sealed class MicrosoftSqlClientDiagnostics : DiagnosticEventObserver
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        private static readonly PropertyFetcher _activityCommand_RequestFetcher = new PropertyFetcher("Command");
        private static readonly PropertyFetcher _exception_ExceptionFetcher = new PropertyFetcher("Exception");

        private readonly MicrosoftSqlClientDiagnosticOptions _options;
        private readonly ConcurrentDictionary<object, ISpan> _spanStorage;

        public MicrosoftSqlClientDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<MicrosoftSqlClientDiagnosticOptions> options)
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
        protected override bool IsSupportedEvent(string eventName) => eventName.StartsWith("Microsoft.");

        protected override IEnumerable<string> HandledEventNames()
        {
            yield return MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandBefore;
            yield return MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandError;
            yield return MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandAfter;
        }

        protected override void HandleEvent(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandBefore:
                    {
                        var cmd = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);

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

                case MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandError:
                    {
                        var cmd = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);
                        var ex = (Exception)_exception_ExceptionFetcher.Fetch(untypedArg);

                        if (_spanStorage.TryRemove(cmd, out var span))
                        {
                            span.SetException(ex);
                            span.Finish();
                        }
                    }
                    break;

                case MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandAfter:
                    {
                        var cmd = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);

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
