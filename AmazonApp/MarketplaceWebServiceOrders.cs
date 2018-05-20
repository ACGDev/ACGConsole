using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazonApp.Model;

namespace AmazonApp
{
    /// <summary>
    /// This contains the Order Retrieval API section of the Marketplace Web Service.
    /// </summary>
    public interface MarketplaceWebServiceOrders
    {

        /// <summary>
        /// Get Order
        /// </summary>
        /// <param name="request">GetOrderRequest request.</param>
        /// <returns>GetOrderResponse response</returns>
        /// <remarks>
        /// This operation takes up to 50 order ids and returns the corresponding orders.
        /// </remarks>
        GetOrderResponse GetOrder(GetOrderRequest request);

        /// <summary>
        /// Get Service Status
        /// </summary>
        /// <param name="request">GetServiceStatusRequest request.</param>
        /// <returns>GetServiceStatusResponse response</returns>
        /// <remarks>
        /// Returns the service status of a particular MWS API section. The operation
        /// 		takes no input.
        /// </remarks>
        GetServiceStatusResponse GetServiceStatus(GetServiceStatusRequest request);

        /// <summary>
        /// List Order Items
        /// </summary>
        /// <param name="request">ListOrderItemsRequest request.</param>
        /// <returns>ListOrderItemsResponse response</returns>
        /// <remarks>
        /// This operation can be used to list the items of the order indicated by the
        ///         given order id (only a single Amazon order id is allowed).
        /// </remarks>
        ListOrderItemsResponse ListOrderItems(ListOrderItemsRequest request);

        /// <summary>
        /// List Order Items By Next Token
        /// </summary>
        /// <param name="request">ListOrderItemsByNextTokenRequest request.</param>
        /// <returns>ListOrderItemsByNextTokenResponse response</returns>
        /// <remarks>
        /// If ListOrderItems cannot return all the order items in one go, it will
        ///         provide a nextToken. That nextToken can be used with this operation to
        ///         retrive the next batch of items for that order.
        /// </remarks>
        ListOrderItemsByNextTokenResponse ListOrderItemsByNextToken(ListOrderItemsByNextTokenRequest request);

        /// <summary>
        /// List Orders
        /// </summary>
        /// <param name="request">ListOrdersRequest request.</param>
        /// <returns>ListOrdersResponse response</returns>
        /// <remarks>
        /// ListOrders can be used to find orders that meet the specified criteria.
        /// </remarks>
        ListOrdersResponse ListOrders(ListOrdersRequest request);

        /// <summary>
        /// List Orders By Next Token
        /// </summary>
        /// <param name="request">ListOrdersByNextTokenRequest request.</param>
        /// <returns>ListOrdersByNextTokenResponse response</returns>
        /// <remarks>
        /// If ListOrders returns a nextToken, thus indicating that there are more orders
        ///         than returned that matched the given filter criteria, ListOrdersByNextToken
        ///         can be used to retrieve those other orders using that nextToken.
        /// </remarks>
        ListOrdersByNextTokenResponse ListOrdersByNextToken(ListOrdersByNextTokenRequest request);
    }
}
