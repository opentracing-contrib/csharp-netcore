using System;
using System.Collections.Generic;
using System.Text;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.Core
{
    public static class SpanExtensions
    {
        /// <summary>
        /// Sets the <see cref="Tags.Error"/> tag and adds information about the <paramref name="exception"/>
        /// to the given <paramref name="span"/>.
        /// </summary>
        public static void SetException(this ISpan span, Exception exception)
        {
            if (span == null || exception == null)
                return;

            Tags.Error.Set(span, true);

            span.Log(new Dictionary<string, object>(3)
            {
                { LogFields.Event, Tags.Error.Key },
                { LogFields.ErrorKind, exception.GetType().Name },
                { LogFields.ErrorObject, exception }
            });
        }
    }
}
