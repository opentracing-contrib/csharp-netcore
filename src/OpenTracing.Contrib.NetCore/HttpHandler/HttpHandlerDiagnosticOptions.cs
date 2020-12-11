using System;
using System.Collections.Generic;
using System.Net.Http;

namespace OpenTracing.Contrib.NetCore.Configuration
{
    public class HttpHandlerDiagnosticOptions : DiagnosticOptions
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
        /// <para/>
        /// If any delegate in the list returns <c>true</c>, the request will be ignored.
        /// </summary>
        public List<Func<HttpRequestMessage, bool>> IgnorePatterns { get; } = new List<Func<HttpRequestMessage, bool>>();

        /// <summary>
        /// A delegates that defines on what requests tracing headers are propagated.
        /// </summary>
        public Func<HttpRequestMessage, bool> InjectEnabled { get; set; }

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

        /// <summary>
        /// Allows the modification of the created span when error occured to e.g. add further tags.
        /// </summary>
        public Action<ISpan, Exception, HttpRequestMessage> OnError { get; set; }

        public HttpHandlerDiagnosticOptions()
        {
            // Default settings

            ComponentName = DefaultComponent;

            IgnorePatterns.Add((request) =>
            {
                IDictionary<string, object> requestOptions;

#if NETCOREAPP2_1 || NETCOREAPP3_1
                requestOptions = request.Properties;
#else 
                requestOptions = request.Options;
#endif

                return requestOptions.ContainsKey(PropertyIgnore);
            });

            OperationNameResolver = (request) =>
            {
                return "HTTP " + request.Method.Method;
            };
        }
    }
}
