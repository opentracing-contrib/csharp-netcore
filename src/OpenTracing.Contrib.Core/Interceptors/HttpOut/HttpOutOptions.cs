using System;
using System.Collections.Generic;
using System.Net.Http;

namespace OpenTracing.Contrib.Core.Configuration
{
    /// <summary>
    /// Configuration for the instrumentation of <see cref="HttpClient"/> calls.
    /// </summary>
    public class HttpOutOptions
    {
        public const string PropertyIgnore = "ot-ignore";

        private Func<HttpRequestMessage, string> _operationNameResolver;

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
            ShouldIgnore.Add((request) =>
            {
                return request.Properties.ContainsKey(PropertyIgnore);
            });

            OperationNameResolver = (request) =>
            {
                return request.Method.Method + "_" + request.RequestUri.AbsolutePath.TrimStart('/');
            };
        }
    }
}
