using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using OpenTracing.Propagation;

namespace OpenTracing.Contrib.NetCore.Interceptors.HttpOut
{
    /// <summary>
    /// A <see cref="ITextMap"/> which allows <see cref="HttpHeaders"/> implementations to be used as carrier objects
    /// for <see cref="ITracer.Inject"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpHeaders"/> is a multi-value dictionary. Since most other platforms represent http headers as regular
    /// dictionaries, this carrier represents it as a regular dictionary to tracer implementations.</remarks>
    internal sealed class HttpHeadersInjectAdapter : ITextMap
    {
        private readonly HttpHeaders _headers;

        public HttpHeadersInjectAdapter(HttpHeaders headers)
        {
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            _headers = headers;
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
