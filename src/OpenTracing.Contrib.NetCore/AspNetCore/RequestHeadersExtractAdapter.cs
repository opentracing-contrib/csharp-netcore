using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using OpenTracing.Propagation;

namespace OpenTracing.Contrib.NetCore.AspNetCore
{
    internal sealed class RequestHeadersExtractAdapter : ITextMap
    {
        private readonly IHeaderDictionary _headers;

        public RequestHeadersExtractAdapter(IHeaderDictionary headers)
        {
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        public void Set(string key, string value)
        {
            throw new NotSupportedException("This class should only be used with ITracer.Extract");
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var kvp in _headers)
            {
                yield return new KeyValuePair<string, string>(kvp.Key, kvp.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
