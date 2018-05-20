using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp
{
    public interface IMwsCall : IMwsWriter
    {
        /// <summary>
        /// Invoke the request synchronously.
        /// <para>Call after writing request body</para>
        /// </summary>
        /// <exception cref="MwsException">Exceptions from invoking the request</exception>
        /// <returns></returns>
        IMwsReader invoke();

        /// <summary>
        /// Get the response metadata header.
        /// <para>Available after invoke returns successfully</para>
        /// </summary>
        /// <returns>Response metadata header</returns>
        MwsResponseHeaderMetadata GetResponseMetadataHeader();
    }
}
