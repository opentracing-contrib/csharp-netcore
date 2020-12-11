using System;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.MicrosoftSqlClient
{
    internal sealed class MicrosoftSqlClientDiagnostics : DiagnosticListenerObserver
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        private static readonly PropertyFetcher _activityCommand_RequestFetcher = new PropertyFetcher("Command");
        private static readonly PropertyFetcher _exception_ExceptionFetcher = new PropertyFetcher("Exception");

        private readonly MicrosoftSqlClientDiagnosticOptions _options;
        private readonly ConcurrentDictionary<object, ISpan> _spanStorage;

        public MicrosoftSqlClientDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<MicrosoftSqlClientDiagnosticOptions> options,
            IOptions<GenericEventOptions> genericEventOptions)
           : base(loggerFactory, tracer, genericEventOptions.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _spanStorage = new ConcurrentDictionary<object, ISpan>();
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        protected override bool IsEnabled(string eventName)
        {
            return eventName.StartsWith("Microsoft.Data.SqlClient");
        }

        protected override void OnNext(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "Microsoft.Data.SqlClient.WriteCommandBefore":
                    {
                        var cmd = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);

                        if (IgnoreEvent(cmd))
                        {
                            Logger.LogDebug("Ignoring SQL command due to IgnorePatterns");
                            return;
                        }

                        string operationName = _options.OperationNameResolver(cmd);

                        var span = Tracer.BuildSpan(operationName)
                            .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                            .WithTag(Tags.Component, _options.ComponentName)
                            .WithTag(Tags.DbInstance, cmd.Connection.Database)
                            .WithTag(Tags.DbStatement, cmd.CommandText)
                            .Start();

                        _spanStorage.TryAdd(cmd, span);
                    }
                    break;

                case "Microsoft.Data.SqlClient.WriteCommandError":
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

                case "Microsoft.Data.SqlClient.WriteCommandAfter":
                    {
                        var cmd = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);

                        if (_spanStorage.TryRemove(cmd, out var span))
                        {
                            span.Finish();
                        }
                    }
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
