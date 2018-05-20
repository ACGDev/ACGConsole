using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazonApp.Model;

namespace AmazonApp
{
    public class MarketplaceWebServiceOrdersSample
    {
        private readonly MarketplaceWebServiceOrders client;

        public MarketplaceWebServiceOrdersSample(MarketplaceWebServiceOrders client)
        {
            this.client = client;
        }

        public ListOrdersResponse InvokeListOrders()
        {
            // Create a request.
            ListOrdersRequest request = new ListOrdersRequest();
            string sellerId = ConfigurationData.SellerId;
            request.SellerId = sellerId;
            string mwsAuthToken = ConfigurationData.MWSToken;
            request.MWSAuthToken = mwsAuthToken;
            DateTime createdAfter = DateTime.Now.AddYears(-1);
            request.CreatedAfter = createdAfter;
            DateTime createdBefore = DateTime.Now.AddDays(-1);
            request.CreatedBefore = createdBefore;
            //DateTime lastUpdatedAfter = new DateTime();
            //request.LastUpdatedAfter = lastUpdatedAfter;
            //DateTime lastUpdatedBefore = new DateTime();
            //request.LastUpdatedBefore = lastUpdatedBefore;
            //List<string> orderStatus = new List<string>();
            //request.OrderStatus = orderStatus;
            List<string> marketplaceId = new List<string>();
            marketplaceId.Add(ConfigurationData.MarketId1);
            marketplaceId.Add(ConfigurationData.MarketId2);
            marketplaceId.Add(ConfigurationData.MarketId3);
            request.MarketplaceId = marketplaceId;
            //List<string> fulfillmentChannel = new List<string>();
            //request.FulfillmentChannel = fulfillmentChannel;
            //List<string> paymentMethod = new List<string>();
            //request.PaymentMethod = paymentMethod;
            //string buyerEmail = "example";
            //request.BuyerEmail = buyerEmail;
            //string sellerOrderId = "example";
            //request.SellerOrderId = sellerOrderId;
            //decimal maxResultsPerPage = 1;
            //request.MaxResultsPerPage = maxResultsPerPage;
            //List<string> tfmShipmentStatus = new List<string>();
            //request.TFMShipmentStatus = tfmShipmentStatus;
            return this.client.ListOrders(request);
        }
        public ListOrderItemsResponse InvokeListOrderItems(string orderId)
        {
            // Create a request.
            ListOrderItemsRequest request = new ListOrderItemsRequest();
            string sellerId = ConfigurationData.SellerId;
            request.SellerId = sellerId;
            string mwsAuthToken = ConfigurationData.MWSToken;
            request.MWSAuthToken = mwsAuthToken;
            string amazonOrderId = orderId;
            request.AmazonOrderId = amazonOrderId;
            return this.client.ListOrderItems(request);
        }
    }
}
