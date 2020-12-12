using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public class MicrosoftSqlClientDiagnosticOptions : DiagnosticOptions
    {
        public static class EventNames
        {
            // https://github.com/dotnet/SqlClient/blob/master/src/Microsoft.Data.SqlClient/netcore/src/Microsoft/Data/SqlClient/SqlClientDiagnosticListenerExtensions.cs

            private const string SqlClientPrefix = "Microsoft.Data.SqlClient.";

            public const string WriteCommandBefore = SqlClientPrefix + nameof(WriteCommandBefore);
            public const string WriteCommandAfter = SqlClientPrefix + nameof(WriteCommandAfter);
            public const string WriteCommandError = SqlClientPrefix + nameof(WriteCommandError);

            public const string WriteConnectionOpenBefore = SqlClientPrefix + nameof(WriteConnectionOpenBefore);
            public const string WriteConnectionOpenAfter = SqlClientPrefix + nameof(WriteConnectionOpenAfter);
            public const string WriteConnectionOpenError = SqlClientPrefix + nameof(WriteConnectionOpenError);

            public const string WriteConnectionCloseBefore = SqlClientPrefix + nameof(WriteConnectionCloseBefore);
            public const string WriteConnectionCloseAfter = SqlClientPrefix + nameof(WriteConnectionCloseAfter);
            public const string WriteConnectionCloseError = SqlClientPrefix + nameof(WriteConnectionCloseError);

            public const string WriteTransactionCommitBefore = SqlClientPrefix + nameof(WriteTransactionCommitBefore);
            public const string WriteTransactionCommitAfter = SqlClientPrefix + nameof(WriteTransactionCommitAfter);
            public const string WriteTransactionCommitError = SqlClientPrefix + nameof(WriteTransactionCommitError);

            public const string WriteTransactionRollbackBefore = SqlClientPrefix + nameof(WriteTransactionRollbackBefore);
            public const string WriteTransactionRollbackAfter = SqlClientPrefix + nameof(WriteTransactionRollbackAfter);
            public const string WriteTransactionRollbackError = SqlClientPrefix + nameof(WriteTransactionRollbackError);
        }

        public const string DefaultComponent = "SqlClient";
        public const string SqlClientPrefix = "sqlClient ";

        private string _componentName = DefaultComponent;
        private List<Func<SqlCommand, bool>> _ignorePatterns;
        private Func<SqlCommand, string> _operationNameResolver;

        /// <summary>
        /// A list of delegates that define whether or not a given SQL command should be ignored.
        /// <para/>
        /// If any delegate in the list returns <c>true</c>, the SQL command will be ignored.
        /// </summary>
        public List<Func<SqlCommand, bool>> IgnorePatterns => _ignorePatterns ??= new List<Func<SqlCommand, bool>>();

        /// <summary>
        /// Allows changing the "component" tag of created spans.
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set => _componentName = value ?? throw new ArgumentNullException(nameof(ComponentName));
        }

        /// <summary>
        /// A delegate that returns the OpenTracing "operation name" for the given command.
        /// </summary>
        public Func<SqlCommand, string> OperationNameResolver
        {
            get
            {
                if (_operationNameResolver == null)
                {
                    // Default value may not be set in the constructor because this would fail
                    // if the target application does not reference SqlClient.
                    _operationNameResolver = (cmd) =>
                    {
                        var commandType = cmd.CommandText?.Split(' ');
                        return $"{SqlClientPrefix}{commandType?.FirstOrDefault()}";
                    };
                }
                return _operationNameResolver;
            }
            set => _operationNameResolver = value ?? throw new ArgumentNullException(nameof(OperationNameResolver));
        }
    }
}
