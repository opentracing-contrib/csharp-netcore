using System;
using System.Data.SqlClient;
using System.Linq;

namespace OpenTracing.Contrib.NetCore.CoreFx
{
    public class SqlClientDiagnosticOptions
    {
        public const string DefaultComponent = "SqlClient";
        public const string SqlClientPrefix = "sqlClient ";

        private string _componentName = DefaultComponent;
        private Func<SqlCommand, string> _operationNameResolver;

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
