using AmazonApp.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AutoCarOperations;
using AutoCarOperations.DAL;
using AutoCarOperations.Model;
using ACG = DCartRestAPIClient;

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
                var invoiceNo = OrderDAL.GetMaxInvoiceNum(ConfigurationData.ConnectionString, "ACGA-");

                foreach (var order in orderResponse.ListOrdersResult.Orders)
                {
                    var orderItemResponse = sample.InvokeListOrderItems(order.AmazonOrderId);
                    order.OrderItem = orderItemResponse.ListOrderItemsResult.OrderItems;
                    var customerId = CreateCustomer(order);
                    var mappedOrder = MapAmazonOrder(order, customerId, ref invoiceNo);
                    var orderRes = RestHelper.AddRecord(mappedOrder, "Orders", ConfigurationData.PrivateKey, ConfigurationData.Token,
                        ConfigurationData.Store);
                    if (orderRes.Status == ACG.ActionStatus.Failed)
                    {
                        //Report as failed
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        public static long CreateCustomer(Order order)
        {
            //check if customer already exist
            var customerOld = CustomerDAL.FindCustomer(ConfigurationData.ConnectionString, cus => cus.email == order.BuyerEmail);
            if (customerOld != null)
            {
                return customerOld.customer_id;
            }
            var nameSplit = order.ShippingAddress.Name.Split(' ');
            var customer = new
            {
                Email= order.BuyerEmail,
                Password= "Amazon"+ Guid.NewGuid().ToString("d").Substring(1, 8),
                BillingCompany = "Amazon",
                BillingFirstName= nameSplit[0],
                BillingLastName= string.Join(" ", nameSplit.Except(new[] { nameSplit[0] })),
                BillingAddress1= order.ShippingAddress.AddressLine1,
                BillingAddress2= (order.ShippingAddress.IsSetAddressLine2()
                ? order.ShippingAddress.AddressLine2
                : "") + (order.ShippingAddress.IsSetAddressLine3() ? order.ShippingAddress.AddressLine3 : ""),
                BillingCity= order.ShippingAddress.City,
                BillingState =order.ShippingAddress.StateOrRegion,
                BillingZipCode= order.ShippingAddress.PostalCode,
                BillingCountry= order.BuyerCounty,
                BillingPhoneNumber =order.ShippingAddress.Phone,
                //BillingTaxID= order.BuyerTaxInfo.,todo: Sam to check
                //ShippingCompany= "Amazon",
                ShippingFirstName= nameSplit[0],
                ShippingLastName= string.Join(" ", nameSplit.Except(new[] { nameSplit[0] })),
                ShippingAddress1 = order.ShippingAddress.AddressLine1,
                ShippingAddress2 = (order.ShippingAddress.IsSetAddressLine2()
                                      ? order.ShippingAddress.AddressLine2
                                      : "") + (order.ShippingAddress.IsSetAddressLine3() ? order.ShippingAddress.AddressLine3 : ""),
                ShippingCity = order.ShippingAddress.City,
                ShippingState = order.ShippingAddress.StateOrRegion,
                ShippingZipCode = order.ShippingAddress.PostalCode,
                ShippingCountry = order.BuyerCounty,
                ShippingPhoneNumber = order.ShippingAddress.Phone,
                //ShippingAddressType= 0,
                //CustomerGroupID= 0,
                Enabled= true,
                MailList = false,
                NonTaxable = false,
                DisableBillingSameAsShipping = true,
                //Comments= "string",
                //AdditionalField1= "string",
                //AdditionalField2= "string",
                //AdditionalField3= "string",
                TotalStoreCredit = 0,
                ResetPassword= true
            };
            var recordInfo = RestHelper.AddRecord(customer, "Customers", ConfigurationData.PrivateKey,
                ConfigurationData.Token, ConfigurationData.Store);
            if (recordInfo.Status == ACG.ActionStatus.Failed)
            {
                //todo: Do Action
            }
            return Convert.ToInt32(recordInfo.ResultSet);
        }
        public static ACG.Order MapAmazonOrder(Order order, long customerId, ref int? invoiceNo)
        {
            var nameSplit = order.ShippingAddress.Name.Split(' ');
            ACG.Order acgOrder = new ACG.Order()
            {
                BillingAddress = order.ShippingAddress.AddressLine1,
                BillingAddress2 = (order.ShippingAddress.IsSetAddressLine2()
                    ? order.ShippingAddress.AddressLine2
                    : "") + (order.ShippingAddress.IsSetAddressLine3() ? order.ShippingAddress.AddressLine3 :""),
                BillingCity = order.ShippingAddress.City,
                BillingCountry = order.ShippingAddress.CountryCode,
                BillingFirstName = nameSplit[0],
                BillingLastName = string.Join(" ", nameSplit.Except(new []{nameSplit[0]})),
                BillingEmail = order.BuyerEmail,
                BillingPhoneNumber = order.ShippingAddress.Phone,
                BillingState = order.ShippingAddress.StateOrRegion,
                BillingZipCode = order.ShippingAddress.PostalCode,
                BillingOnLinePayment = false,
                BillingPaymentMethodID = "50",
                UserID = "storeadmin1",
                CustomerID = customerId,
                OrderAmount = Convert.ToDouble(order.OrderTotal.Amount),
                OrderDate = order.PurchaseDate,
                InvoiceNumberPrefix = "ACGA-",
                InvoiceNumber = invoiceNo+1,
                Referer = "AMAZON ORDER",
                SalesPerson = "",
                CardNumber = "-1",
                CardName = "Amazon Items",
                ShipmentList = new List<ACG.Shipment>
                {
                   new ACG.Shipment()
                   {
                       ShipmentAddress = order.ShippingAddress.AddressLine1,
                       ShipmentAddress2 = (order.ShippingAddress.IsSetAddressLine2()
                                              ? order.ShippingAddress.AddressLine2
                                              : "") + (order.ShippingAddress.IsSetAddressLine3() ? order.ShippingAddress.AddressLine3 :""),
                       ShipmentCity = order.ShippingAddress.City,
                       ShipmentCountry = order.ShippingAddress.County,
                       ShipmentEmail = order.BuyerEmail,
                       ShipmentFirstName = nameSplit[0],
                       ShipmentLastName = string.Join(" ", nameSplit.Except(new []{nameSplit[0]})),
                       ShipmentPhone = order.ShippingAddress.Phone,
                       ShipmentShippedDate = order.LatestShipDate.ToLongDateString(),
                       ShipmentState = order.ShippingAddress.StateOrRegion,
                       ShipmentZipCode = order.ShippingAddress.PostalCode
                   }
                },
                OrderItemList = new List<ACG.OrderItem>()
            };

            foreach (var item in order.OrderItem)
            {
                var orderItem = ProductDAL.FindOrderFromSKU(ConfigurationData.ConnectionString, item.SellerSKU);
                acgOrder.OrderItemList.Add(new ACG.OrderItem
                {
                    ItemQuantity = Convert.ToDouble(item.QuantityOrdered),
                    ItemUnitCost = Convert.ToDouble(item.ItemPrice),
                    ItemID = orderItem.Item2.ItemID,
                    CatalogID = orderItem.Item1.catalogid,
                    //ItemDescription = item.
                    //ItemDescription = ,
                    ItemWeight = Convert.ToDouble(item.ProductInfo.NumberOfItems),
                    //
                    //Order_Items Table : Add extra field additionalField4 >
                    //ResourceCode and Message
                });
            }
            return acgOrder;
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
        public static string ConnectionString = ConfigurationManager.ConnectionStrings["mysqlconnection"].ConnectionString;
        internal static string PrivateKey = ConfigurationManager.AppSettings["PrivateKey"];
        internal static string Token = ConfigurationManager.AppSettings["Token"];
        internal static string Store = ConfigurationManager.AppSettings["Store"];
    }
}
