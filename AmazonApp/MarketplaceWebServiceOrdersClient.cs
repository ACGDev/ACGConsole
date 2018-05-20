using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazonApp.Model;

namespace AmazonApp
{
    /// <summary>
    /// MarketplaceWebServiceOrdersClient is an implementation of MarketplaceWebServiceOrders
    /// </summary>
    public class MarketplaceWebServiceOrdersClient : MarketplaceWebServiceOrders
    {

        private const string libraryVersion = "2018-01-31";

        private string servicePath;

        private MwsConnection connection;

        /// <summary>
        /// Create client.
        /// </summary>
        /// <param name="accessKey">Access Key</param>
        /// <param name="secretKey">Secret Key</param>
        /// <param name="applicationName">Application Name</param>
        /// <param name="applicationVersion">Application Version</param>
        /// <param name="config">configuration</param>
        public MarketplaceWebServiceOrdersClient(
            string accessKey,
            string secretKey,
            string applicationName,
            string applicationVersion,
            MarketplaceWebServiceOrdersConfig config)
        {
            connection = config.CopyConnection();
            connection.AwsAccessKeyId = accessKey;
            connection.AwsSecretKeyId = secretKey;
            connection.ApplicationName = applicationName;
            connection.ApplicationVersion = applicationVersion;
            connection.LibraryVersion = libraryVersion;
            servicePath = config.ServicePath;
        }

        /// <summary>
        /// Create client.
        /// </summary>
        /// <param name="accessKey">Access Key</param>
        /// <param name="secretKey">Secret Key</param>
        /// <param name="config">configuration</param>
        public MarketplaceWebServiceOrdersClient(String accessKey, String secretKey, MarketplaceWebServiceOrdersConfig config)
        {
            connection = config.CopyConnection();
            connection.AwsAccessKeyId = accessKey;
            connection.AwsSecretKeyId = secretKey;
            connection.LibraryVersion = libraryVersion;
            servicePath = config.ServicePath;
        }

        /// <summary>
        /// Create client.
        /// </summary>
        /// <param name="accessKey">Access Key</param>
        /// <param name="secretKey">Secret Key</param>
        public MarketplaceWebServiceOrdersClient(String accessKey, String secretKey)
            : this(accessKey, secretKey, new MarketplaceWebServiceOrdersConfig())
        {
        }

        /// <summary>
        /// Create client.
        /// </summary>
        /// <param name="accessKey">Access Key</param>
        /// <param name="secretKey">Secret Key</param>
        /// <param name="applicationName">Application Name</param>
        /// <param name="applicationVersion">Application Version</param>
        public MarketplaceWebServiceOrdersClient(
            String accessKey,
            String secretKey,
            String applicationName,
            String applicationVersion)
            : this(accessKey, secretKey, applicationName,
                applicationVersion, new MarketplaceWebServiceOrdersConfig())
        {
        }

        public GetOrderResponse GetOrder(GetOrderRequest request)
        {
            return connection.Call(
                new MarketplaceWebServiceOrdersClient.Request<GetOrderResponse>("GetOrder", typeof(GetOrderResponse), servicePath),
                request);
        }

        public GetServiceStatusResponse GetServiceStatus(GetServiceStatusRequest request)
        {
            return connection.Call(
                new MarketplaceWebServiceOrdersClient.Request<GetServiceStatusResponse>("GetServiceStatus", typeof(GetServiceStatusResponse), servicePath),
                request);
        }

        public ListOrderItemsResponse ListOrderItems(ListOrderItemsRequest request)
        {
            return connection.Call(
                new MarketplaceWebServiceOrdersClient.Request<ListOrderItemsResponse>("ListOrderItems", typeof(ListOrderItemsResponse), servicePath),
                request);
        }

        public ListOrderItemsByNextTokenResponse ListOrderItemsByNextToken(ListOrderItemsByNextTokenRequest request)
        {
            return connection.Call(
                new MarketplaceWebServiceOrdersClient.Request<ListOrderItemsByNextTokenResponse>("ListOrderItemsByNextToken", typeof(ListOrderItemsByNextTokenResponse), servicePath),
                request);
        }

        public ListOrdersResponse ListOrders(ListOrdersRequest request)
        {
            return connection.Call(
                new MarketplaceWebServiceOrdersClient.Request<ListOrdersResponse>("ListOrders", typeof(ListOrdersResponse), servicePath),
                request);
        }

        public ListOrdersByNextTokenResponse ListOrdersByNextToken(ListOrdersByNextTokenRequest request)
        {
            return connection.Call(
                new MarketplaceWebServiceOrdersClient.Request<ListOrdersByNextTokenResponse>("ListOrdersByNextToken", typeof(ListOrdersByNextTokenResponse), servicePath),
                request);
        }

        private class Request<R> : IMwsRequestType<R> where R : IMwsObject
        {
            private string operationName;
            private Type responseClass;
            private string servicePath;

            public Request(string operationName, Type responseClass, string servicePath)
            {
                this.operationName = operationName;
                this.responseClass = responseClass;
                this.servicePath = servicePath;
            }

            public string ServicePath
            {
                get { return servicePath; }
            }

            public string OperationName
            {
                get { return operationName; }
            }

            public Type ResponseClass
            {
                get { return responseClass; }
            }

            public MwsException WrapException(Exception cause)
            {
                return new MarketplaceWebServiceOrdersException(cause);
            }

            public void SetResponseHeaderMetadata(IMwsObject response, MwsResponseHeaderMetadata rhmd)
            {
                ((IMWSResponse)response).ResponseHeaderMetadata = new ResponseHeaderMetadata(rhmd);
            }

        }
    }
}
