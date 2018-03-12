using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OpenTracing.Contrib.NetCore.EntityFrameworkCore
{
    public class EntityFrameworkCoreOptions
    {
        public const string DefaultComponent = "EFCore";

        private string _componentName = DefaultComponent;
        private Func<CommandEventData, string> _operationNameResolver;

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
        public Func<CommandEventData, string> OperationNameResolver
        {
            get
            {
                if (_operationNameResolver == null)
                {
                    // Default value may not be set in the constructor because this would fail
                    // if the target application does not reference EFCore.
                    _operationNameResolver = (data) => "DB " + data.ExecuteMethod.ToString();
                }
                return _operationNameResolver;
            }
            set => _operationNameResolver = value ?? throw new ArgumentNullException(nameof(OperationNameResolver));
        }
    }
}
