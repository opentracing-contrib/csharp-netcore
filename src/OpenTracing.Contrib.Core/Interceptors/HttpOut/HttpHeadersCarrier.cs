using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using OpenTracing.Propagation;

namespace OpenTracing.Contrib.Core.Interceptors.HttpOut
{
    /// <summary>
    /// A <see cref="ITextMap"/> which allows <see cref="HttpHeaders"/> implementations to be used as carrier objects.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpHeaders"/> is a multi-value dictionary. Since most other platforms represent http headers as regular
    /// dictionaries, this carrier represents it as a regular dictionary to tracer implementations.</remarks>
    internal sealed class HttpHeadersCarrier : ITextMap
    {
        private readonly HttpHeaders _headers;

        public HttpHeadersCarrier(HttpHeaders headers)
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
            foreach (var kvp in _headers)
            {
                // TODO Is string.Join() the correct behavior?
                yield return new KeyValuePair<string, string>(kvp.Key, string.Join(",", kvp.Value));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
