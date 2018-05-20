using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp
{
    /// <summary>
    /// Defines metadata for an operation.
    /// <para>This includes the service, version, operation name, response object class
    /// and call parameters.</para>
    /// </summary>
    public interface IMwsRequestType<Response> where Response : IMwsObject
    {
        /// <summary>
        /// Get the class that will be thrown for an exception response.
        /// </summary>
        /// <returns></returns>
        MwsException WrapException(Exception e);

        /// <summary>
        /// Get the operation name that identifies the operation within the service
        /// </summary>
        string OperationName { get; }

        /// <summary>
        /// Get the class that will hold a successful response
        /// </summary>
        Type ResponseClass { get; }

        /// <summary>
        /// Get the service path string that identifies the service and version
        /// to call on the server
        /// </summary>
        string ServicePath { get; }

        /// <summary>
        /// wrap response header metadata and set into response object
        /// </summary>
        /// <param name="response"></param>
        /// <param name="rhmd"></param>
        void SetResponseHeaderMetadata(IMwsObject response, MwsResponseHeaderMetadata rhmd);
    }
}
