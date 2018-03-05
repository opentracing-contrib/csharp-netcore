using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace OpenTracing.Contrib.AspNetCore.Configuration
{
    public class RequestInOptions
    {
        public const string DefaultComponent = "HttpIn";

        private string _componentName;
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
        /// </summary>
        public List<Func<HttpContext, bool>> ShouldIgnore { get; } = new List<Func<HttpContext, bool>>();

        /// <summary>
        /// A delegate that returns the OpenTracing "operation name" for the given request.
        /// </summary>
        public Func<HttpContext, string> OperationNameResolver
        {
            get => _operationNameResolver;
            set => _operationNameResolver = value ?? throw new ArgumentNullException(nameof(OperationNameResolver));
        }

        /// <summary>
        /// Allows the modification of the created span to e.g. add further tags.
        /// </summary>
        public Action<ISpan, HttpContext> OnRequest { get; set; }

        public RequestInOptions()
        {
            // Default settings

            ComponentName = DefaultComponent;

            OperationNameResolver = (httpContext) =>
            {
                return "HTTP " + httpContext.Request.Method;
            };
        }
    }
}
