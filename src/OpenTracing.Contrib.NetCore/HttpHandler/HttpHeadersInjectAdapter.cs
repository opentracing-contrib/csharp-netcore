using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using OpenTracing.Propagation;

namespace OpenTracing.Contrib.NetCore.HttpHandler
{
    internal sealed class HttpHeadersInjectAdapter : ITextMap
    {
        private readonly HttpHeaders _headers;

        public HttpHeadersInjectAdapter(HttpHeaders headers)
        {
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        public void Set(string key, string value)
        {
            if (_headers.Contains(key))
            {
                _headers.Remove(key);
            }

            _headers.Add(key, value);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            throw new NotSupportedException("This class should only be used with ITracer.Inject");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
