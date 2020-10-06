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
                        var args = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);

                        if (IgnoreEvent(args))
                        {
                            Logger.LogDebug("Ignoring SQL command due to IgnorePatterns");
                            return;
                        }

                        string operationName = _options.OperationNameResolver(args);

                        Tracer.BuildSpan(operationName)
                            .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                            .WithTag(Tags.Component, _options.ComponentName)
                            .WithTag(Tags.DbInstance, args.Connection.Database)
                            .WithTag(Tags.DbStatement, args.CommandText)
                            .StartActive();
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandError":
                    {
                        Exception ex = (Exception)_exception_ExceptionFetcher.Fetch(untypedArg);

                        DisposeActiveScope(isScopeRequired: false, exception: ex);
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandAfter":
                    {
                        DisposeActiveScope(isScopeRequired: false);
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
