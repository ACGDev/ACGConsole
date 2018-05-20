using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp
{
    class Program
    {
        static void Main(string[] args)
        {
            MarketplaceWebServiceOrdersConfig config = new MarketplaceWebServiceOrdersConfig();
            config.ServiceURL = ConfigurationData.ServiceURL;
            // Set other client connection configurations here if needed
            // Create the client itself
            MarketplaceWebServiceOrders client = new MarketplaceWebServiceOrdersClient(ConfigurationData.AccessKey, ConfigurationData.SecretKey, ConfigurationData.AppName, ConfigurationData.Version, config);
            MarketplaceWebServiceOrdersSample sample = new MarketplaceWebServiceOrdersSample(client);
            try
            {
                IMWSResponse response = null;
                response = sample.InvokeListOrders();
                ResponseHeaderMetadata rhmd = response.ResponseHeaderMetadata;
                Console.WriteLine("RequestId: " + rhmd.RequestId);
                Console.WriteLine("Timestamp: " + rhmd.Timestamp);
                var orderResponse = response as ListOrdersResponse;
                foreach (var order in orderResponse.ListOrdersResult.Orders)
                {
                    var orderItemResponse = sample.InvokeListOrderItems(order.AmazonOrderId);
                    order.OrderItem = orderItemResponse.ListOrderItemsResult.OrderItems;
                }
            }
            catch (Exception ex)
            {
                
            }
        }
    }

    static class ConfigurationData
    {
        public static string AccessKey = ConfigurationManager.AppSettings["AccessKey"];
        public static string SecretKey = ConfigurationManager.AppSettings["SecretKey"];
        public static string AppName = ConfigurationManager.AppSettings["AppName"];
        public static string Version = ConfigurationManager.AppSettings["Version"];
        public static string ServiceURL = ConfigurationManager.AppSettings["ServiceURL"];
        public static string SellerId = ConfigurationManager.AppSettings["SellerId"];
        public static string MWSToken = ConfigurationManager.AppSettings["MWSToken"];
        public static string MarketId1 = ConfigurationManager.AppSettings["MarketId1"];
        public static string MarketId2 = ConfigurationManager.AppSettings["MarketId2"];
        public static string MarketId3 = ConfigurationManager.AppSettings["MarketId3"];
    }
}
