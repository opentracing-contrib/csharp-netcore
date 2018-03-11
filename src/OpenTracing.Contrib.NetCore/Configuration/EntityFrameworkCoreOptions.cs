using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OpenTracing.Contrib.NetCore.Configuration
{

    public class EntityFrameworkCoreOptions
    {
        public const string DefaultComponent = "EFCore";

        private string _componentName;
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
            get => _operationNameResolver;
            set => _operationNameResolver = value ?? throw new ArgumentNullException(nameof(OperationNameResolver));
        }

        public EntityFrameworkCoreOptions()
        {
            // Default settings

            ComponentName = DefaultComponent;

            OperationNameResolver = (data) =>
            {
                return "DB " + data.ExecuteMethod.ToString();
            };
        }
    }
}
