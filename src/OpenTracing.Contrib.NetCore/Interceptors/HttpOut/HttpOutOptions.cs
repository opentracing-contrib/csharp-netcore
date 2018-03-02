using System;
using System.Collections.Generic;
using System.Net.Http;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    /// <summary>
    /// Configuration options for the instrumentation of outgoing HTTP calls.
    /// </summary>
    public class HttpOutOptions
    {
        public const string PropertyIgnore = "ot-ignore";

        public const string DefaultComponent = "HttpOut";

        private string _componentName;
        private Func<HttpRequestMessage, string> _operationNameResolver;

        /// <summary>
        /// Allows changing the "component" tag of created spans.
        /// </summary>
        public string ComponentName
        {
            get => _componentName;
            set => _componentName = value ?? throw new ArgumentNullException(nameof(ComponentName));
        }

        /// <summary>
        /// A list of delegates that define whether or not a given request should be ignored.
        /// </summary>
        public List<Func<HttpRequestMessage, bool>> ShouldIgnore { get; } = new List<Func<HttpRequestMessage, bool>>();

        /// <summary>
        /// A delegate that returns the OpenTracing "operation name" for the given request.
        /// </summary>
        public Func<HttpRequestMessage, string> OperationNameResolver
        {
            get => _operationNameResolver;
            set => _operationNameResolver = value ?? throw new ArgumentNullException(nameof(OperationNameResolver));
        }

        /// <summary>
        /// Allows the modification of the created span to e.g. add further tags.
        /// </summary>
        public Action<ISpan, HttpRequestMessage> OnRequest { get; set; }

        public HttpOutOptions()
        {
            // Default settings

            ComponentName = DefaultComponent;

            ShouldIgnore.Add((request) =>
            {
                return request.Properties.ContainsKey(PropertyIgnore);
            });

            OperationNameResolver = (request) =>
            {
                return "HTTP " + request.Method.Method;
            };
        }
    }
}
