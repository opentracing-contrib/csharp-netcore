using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.CoreFx
{
    internal sealed class SqlClientDiagnostics : DiagnosticListenerObserver
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        private static readonly PropertyFetcher _activityCommand_RequestFetcher = new PropertyFetcher("Command");
        private static readonly PropertyFetcher _exception_ExceptionFetcher = new PropertyFetcher("Exception");

        private readonly SqlClientDiagnosticOptions _options;
        private readonly ConcurrentDictionary<object, ISpan> _spanStorage;

        public SqlClientDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<SqlClientDiagnosticOptions> options,
            IOptions<GenericEventOptions> genericEventOptions)
           : base(loggerFactory, tracer, genericEventOptions.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _spanStorage = new ConcurrentDictionary<object, ISpan>();
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        protected override bool IsEnabled(string eventName)
        {
            return eventName.StartsWith("System.Data.SqlClient");
        }

        protected override void OnNext(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "System.Data.SqlClient.WriteCommandBefore":
                    {
                        var cmd = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);

                        var activeSpan = Tracer.ActiveSpan;
                        if (activeSpan == null && !_options.StartRootSpans)
                        {
                            Logger.LogDebug("Ignoring event (StartRootSpans=false)");
                            return;
                        }
                        if (IgnoreEvent(cmd))
                        {
                            Logger.LogDebug("Ignoring SQL command due to IgnorePatterns");
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
                        var cmd = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);
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
