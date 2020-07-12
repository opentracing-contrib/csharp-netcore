using System;
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

        public SqlClientDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<SqlClientDiagnosticOptions> options,
            IOptions<GenericEventOptions> genericEventOptions)
           : base(loggerFactory, tracer, genericEventOptions.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        protected override void OnNext(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "System.Data.SqlClient.WriteCommandBefore":
                    {
                        var sqlCommand = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);
                        if (!_options.SpanStartDecider(sqlCommand))
                            return;

                        string operationName = _options.OperationNameResolver(sqlCommand);

                        Tracer.BuildSpan(operationName)
                            .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                            .WithTag(Tags.Component, _options.ComponentName)
                            .WithTag(Tags.DbInstance, sqlCommand.Connection.Database)
                            .WithTag(Tags.DbStatement, sqlCommand.CommandText)
                            .StartActive();
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandError":
                    {
                        Exception ex = (Exception)_exception_ExceptionFetcher.Fetch(untypedArg);
                        var sqlCommand = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);
                        if (!_options.SpanStartDecider(sqlCommand))
                            return;
                        DisposeActiveScope(isScopeRequired: true, exception: ex);
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandAfter":
                    {
                        var sqlCommand = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);
                        if (!_options.SpanStartDecider(sqlCommand))
                            return;
                        DisposeActiveScope(isScopeRequired: true);
                    }
                    break;
            }
        }
    }
}
