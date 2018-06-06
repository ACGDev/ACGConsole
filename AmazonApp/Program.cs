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
            // SAM: update customer list fist
            //CustomerDAL.AddCustomer(config);

            MarketplaceWebServiceOrdersConfig config = new MarketplaceWebServiceOrdersConfig();
            config.ServiceURL = ConfigurationData.ServiceURL;
            
            // Set other client connection configurations here if needed
            // Create the client itself
            MarketplaceWebServiceOrders client = new MarketplaceWebServiceOrdersClient(ConfigurationData.AccessKey, ConfigurationData.SecretKey, ConfigurationData.AppName, ConfigurationData.Version, config);
            MarketplaceWebServiceOrdersSample amazonOrders = new MarketplaceWebServiceOrdersSample(client);
            try
            {
                IMWSResponse response = null;
                response = amazonOrders.InvokeListOrders();
                ResponseHeaderMetadata rhmd = response.ResponseHeaderMetadata;
                Console.WriteLine("RequestId: " + rhmd.RequestId);
                Console.WriteLine("Timestamp: " + rhmd.Timestamp);
                var orderResponse = response as ListOrdersResponse;
                var invoiceNo = OrderDAL.GetMaxInvoiceNum(ConfigurationData.ConnectionString, "ACGA-");

                foreach (var order in orderResponse.ListOrdersResult.Orders)
                {
                    order.BuyerEmail = order.BuyerEmail.Replace("marketplace.amazon.com", "AutoCareGuys.com");
                    //if (order.OrderStatus.ToLower() != "unshipped")
                    //    continue;
                    var orderItemResponse = amazonOrders.InvokeListOrderItems(order.AmazonOrderId);
                    order.OrderItem = orderItemResponse.ListOrderItemsResult.OrderItems;
                    var customerId = CreateCustomer(order);  // ** SAM: We should check if the order exists before this. Can set Amaz order no as PO
                    var mappedOrder = MapAmazonOrder(order, customerId, ref invoiceNo);

                }
            }
            catch (Exception ex)
            {
                
            }
        }

        public static long CreateCustomer(Order order)
        {
            //check if customer already exist
            // ** SAM: Need to discuss using Amazon email directly -> as it will send an email to customer automatically from 3DCart
            var customerOld = CustomerDAL.FindCustomer(ConfigurationData.ConnectionString, cus => cus.email == order.BuyerEmail);
            if (customerOld != null)
            {
                return customerOld.customer_id;
            }
            var nameSplit = order.ShippingAddress.Name.Split(' ');
            var customer = new ACG.Customer
            {
                Email= order.BuyerEmail,
                Password= "Amazon"+ Guid.NewGuid().ToString("d").Substring(1, 8),
                BillingCompany = "",
                BillingFirstName= nameSplit[0],
                BillingLastName= string.Join(" ", nameSplit.Except(new[] { nameSplit[0] })),
                BillingAddress1= order.ShippingAddress.AddressLine1,
                BillingAddress2= (order.ShippingAddress.IsSetAddressLine2()
                ? order.ShippingAddress.AddressLine2
                : "") + (order.ShippingAddress.IsSetAddressLine3() ? order.ShippingAddress.AddressLine3 : ""),
                BillingCity= order.ShippingAddress.City,
                BillingState =order.ShippingAddress.StateOrRegion,
                BillingZipCode= order.ShippingAddress.PostalCode,
                BillingCountry= order.ShippingAddress.CountryCode, // order.BuyerCounty
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
                ShippingCountry = order.ShippingAddress.CountryCode, // order.BuyerCounty
                ShippingPhoneNumber = order.ShippingAddress.Phone,
                //ShippingAddressType= 0,
                //CustomerGroupID= 0,  //** SAM: Need to set to Customer Group Amazon from 3D cart (customertype ?)
                Enabled= true,
                MailList = false,
                NonTaxable = true,
                DisableBillingSameAsShipping = true,
                CustomerGroupID = 10,
                //Comments= "string",
                //AdditionalField1= "string",
                //AdditionalField2= "string",
                //AdditionalField3= "string",
                TotalStoreCredit = ""
            };

            var recordInfo = RestHelper.AddRecord(customer, "Customers", ConfigurationData.PrivateKey,
                ConfigurationData.Token, ConfigurationData.Store);
            if (recordInfo.Status == ACG.ActionStatus.Failed)
            {
                //todo: Do Action ** SAM: Check that recordinfo.Resultset is null - send error email as in JFW order processing
            }
            var customerId = Convert.ToInt32(recordInfo.ResultSet);
            customer.CustomerID = customerId;
            CustomerDAL.AddCustomers(ConfigurationData.ConnectionString, new List<ACG.Customer>() { customer }, new List<ACG.CustomerGroup>());
            return customerId;
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
                PONo = order.AmazonOrderId,
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
                       ShipmentCountry = order.ShippingAddress.CountryCode,
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

                // ** SAM: Make sure we have AmazonOrderItemID stored in some table for every order item

                // ** SAM: From Amazon, find Manufacturer Part no for this ASIN. Then use that MPN to map our part no.
                // Search our ASIN table, if the needed, insert this row. Or update.
                // SAM: It will be better if we can capture the SKU (new) coming back from Amazon - Can put it in ASIN table.

                // OR - simply map ASIN with our ASIN table and get the part no.
                var orderItem = ProductDAL.FindOrderFromASIN(ConfigurationData.ConnectionString, item.ASIN);
                if (orderItem == null || orderItem.Item1 == null
                    || orderItem.Item2 == null || orderItem.Item3 == null)
                {
                    //todo: Query 3d cart API if catalogid is null.
                    continue;
                }
                //** SAM Need to check failure condition that orderItem may be null
                // *** Also these set of SKU's are new and probably we need a new field in the product table for this.
                acgOrder.OrderItemList.Add(new ACG.OrderItem
                {
                    ItemQuantity = Convert.ToDouble(item.QuantityOrdered),
                    ItemUnitCost = Convert.ToDouble(item.ItemPrice),
                    ItemID = orderItem.Item1,
                    CatalogID = orderItem.Item3,
                    ItemOptions = orderItem.Item2,
                    //ItemDescription = item.
                    //ItemDescription = ,
                    ItemWeight = Convert.ToDouble(item.ProductInfo.NumberOfItems),
                    //
                    //Order_Items Table : Add extra field additionalField4 >
                    //ResourceCode and Message
                });
            }
            var orderRes = RestHelper.AddRecord(acgOrder, "Orders", ConfigurationData.PrivateKey, ConfigurationData.Token,
                ConfigurationData.Store);
            if (orderRes.Status == ACG.ActionStatus.Failed)
            {
                //Report as failed
            }
            var orderId = Convert.ToInt32(orderRes.ResultSet);
            acgOrder.OrderID = orderId;
            OrderDAL.Map_n_Add_ExtOrders(ConfigurationData.ConnectionString, "", new List<ACG.Order>() {acgOrder});
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
