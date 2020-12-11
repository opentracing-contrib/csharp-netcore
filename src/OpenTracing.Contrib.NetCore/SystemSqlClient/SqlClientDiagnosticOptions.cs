using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public class SqlClientDiagnosticOptions : DiagnosticOptions
    {
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
