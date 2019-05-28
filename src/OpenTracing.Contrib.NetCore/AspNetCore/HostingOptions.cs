using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    public class HostingOptions
    {
        public const string DefaultComponent = "HttpIn";

        // Variables are lazily instantiated to prevent the app from crashing if the required assemblies are not referenced.

        private string _componentName = DefaultComponent;
        private List<Func<HttpContext, bool>> _ignorePatterns;
        private Func<HttpContext, string> _operationNameResolver;


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
        public List<Func<HttpContext, bool>> IgnorePatterns
        {
            get
            {
                if (_ignorePatterns == null)
                {
                    _ignorePatterns = new List<Func<HttpContext, bool>>();
                }
                return _ignorePatterns;
            }
        }

        /// <summary>
        /// A delegates that defines from which requests tracing headers are extracted.
        /// </summary>
        public Func<HttpContext, bool> ExtractEnabled { get; set; }

        /// <summary>
        /// A delegate that returns the OpenTracing "operation name" for the given request.
        /// </summary>
        public Func<HttpContext, string> OperationNameResolver
        {
            get
            {
                if (_operationNameResolver == null)
                {
                    _operationNameResolver = (httpContext) => "HTTP " + httpContext.Request.Method;
                }
                return _operationNameResolver;
            }
            set => _operationNameResolver = value ?? throw new ArgumentNullException(nameof(OperationNameResolver));
        }

        /// <summary>
        /// Allows the modification of the created span to e.g. add further tags.
        /// </summary>
        public Action<ISpan, HttpContext> OnRequest { get; set; }

        /// <summary>
        /// Allows the modification of the created span when error occured to e.g. add further tags.
        /// </summary>
        public Action<ISpan, Exception, HttpContext> OnError { get; set; }
    }
}
