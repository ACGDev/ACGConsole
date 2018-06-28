using AmazonApp.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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
                    if (order.OrderStatus.ToLower() != "unshipped")
                        continue;

                    //** SAM: Find if this order already exists (but unshipped)
                    var existingOrder = OrderDAL.GetOrderFromPO(ConfigurationData.ConnectionString, order.AmazonOrderId );
                    if ( null != existingOrder ) continue;

                    // Check if this Amazon order has been already entered
                    var orderItemResponse = amazonOrders.InvokeListOrderItems(order.AmazonOrderId);
                    order.OrderItem = orderItemResponse.ListOrderItemsResult.OrderItems;
                    var customerId = CreateCustomer(order);  
                    var mappedOrder = MapAmazonOrder(order, customerId, ref invoiceNo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred "+ex.Message);
                Console.ReadKey();
            }
        }

        public static long CreateCustomer(Order order)
        {
            //check if customer already exist
            var customerOld = CustomerDAL.FindCustomer(ConfigurationData.ConnectionString, cus => cus.billing_address == order.ShippingAddress.AddressLine1 && cus.billing_zip == order.ShippingAddress.PostalCode);
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
                Enabled= true,
                MailList = false,
                NonTaxable = true,
                DisableBillingSameAsShipping = true,
                CustomerGroupID = 13,  // Amazon
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
                MandrillMail.SendEmail(ConfigurationData.MandrilAPIKey, "Order Has to be processed manually. ",
                    "Order Has to be processed manually. ASIN information is not available. The order no is:" + order.AmazonOrderId,
                    "sales@autocareguys.com");
            }
            var customerId = Convert.ToInt32(recordInfo.ResultSet);
            customer.CustomerID = customerId;
            CustomerDAL.AddCustomers(ConfigurationData.ConnectionString, new List<ACG.Customer>() { customer }, new List<ACG.CustomerGroup>());
            return customerId;
        }
        public static ACG.Order MapAmazonOrder(Order order, long customerId, ref int? invoiceNo)
        {
            var nameSplit = order.ShippingAddress.Name.Split(' ');
            invoiceNo += 1;
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
                BillingPhoneNumber = order.ShippingAddress.Phone == null ? "111-111-1111" : order.ShippingAddress.Phone,
                BillingState = order.ShippingAddress.StateOrRegion,
                BillingZipCode = order.ShippingAddress.PostalCode,
                BillingOnLinePayment = false,
                BillingPaymentMethodID = "50",
                BillingPaymentMethod = "Purchase Order",
                UserID = "storeadmin1",
                PONo = order.AmazonOrderId,
                CustomerID = customerId,
                OrderAmount = Convert.ToDouble(order.OrderTotal.Amount),
                OrderDate = order.PurchaseDate,
                InvoiceNumberPrefix = "ACGA-",
                InvoiceNumber = invoiceNo,
                Referer = "http://www.amazon.com",
                SalesPerson = "",
                CardNumber = "-1",
                CardName = "Amazon Items",
                OrderStatusID = 1,
                CustomerComments = order.AmazonOrderId,
                OrderID = 0,
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
                       ShipmentPhone = order.ShippingAddress.Phone ?? "111-111-1111",
                       // SAM: This is tobe shipped By date. we may need a field for this. ShipmentShippedDate = order.LatestShipDate.ToLongDateString(),
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
                var orderItem = ProductDAL.FindOrderFromASIN(ConfigurationData.ConnectionString, item.ASIN, item.SellerSKU);
                if (orderItem == null || orderItem.catalogid == null
                    || orderItem.ItemId == null )
                {
                    MandrillMail.SendEmail(ConfigurationData.MandrilAPIKey, "Order Has to be processed manually. ",
                        "Order Has to be processed manually. OrderItem ASIN information is null. The order no is:" + order.AmazonOrderId,
                        "sales@autocareguys.com");
                    //todo: SAM: Query 3d cart API if catalogid is null. Map "CK_"+CK_ASIN.ItemId with SKU of 3D Cart
                    continue;
                }
                //** SAM Need to check failure condition that orderItem may be null
                // *** Also these set of SKU's are new and probably we need a new field in the product table for this.
                acgOrder.OrderItemList.Add(new ACG.OrderItem
                {
                    ItemQuantity = Convert.ToDouble(item.QuantityOrdered),
                    ItemUnitPrice = Convert.ToDouble(item.ItemPrice.Amount),
                    //ItemOptionPrice = Convert.ToDouble(item.ItemPrice.Amount),
                    ItemID = orderItem.ItemId,
                    CatalogID = orderItem.catalogid,
                    ItemOptions = orderItem.VariantId,
                    ItemDescription = item.Title,
                    // ItemWeight = Convert.ToDouble(item.,
                    ItemAdditionalField1 = orderItem.ResourceCode
                    //
                    //Order_Items Table : Add extra field additionalField4 >
                    //ResourceCode and Message
                });
                
            }
            
            
            var orderRes = RestHelper.AddRecord(acgOrder, "Orders", ConfigurationData.PrivateKey, ConfigurationData.Token,
                ConfigurationData.Store);
            if (orderRes.Status == ACG.ActionStatus.Failed)
            {
                MandrillMail.SendEmail(ConfigurationData.MandrilAPIKey, "Order Has to be processed manually",
                    "Order Has to be processed manually. The order no is:" + order.AmazonOrderId,
                    "sales@autocareguys.com");
            }

            var orderId = Convert.ToInt32(orderRes.ResultSet);
            acgOrder.OrderID = orderId;
            var orderList = OrderDAL.Map_n_Add_ExtOrders(ConfigurationData.ConnectionString, "", new List<ACG.Order>() { acgOrder });

            //OrderDAL.PlaceOrder(new AutoCarOperations.Model.ConfigurationData()
            //{
            //    ConnectionString = ConfigurationData.ConnectionString,
            //    CKOrderFolder = ConfigurationData.CKOrderFolder,
            //    MandrilAPIKey = ConfigurationData.MandrilAPIKey,
            //    FTPAddress = ConfigurationData.FTPAddress,
            //    FTPUserName = ConfigurationData.FTPUserName,
            //    FTPPassword = ConfigurationData.FTPPassword,
            //}, false, true, true, orderList);

            // SAM: No need to sync orders - just place it to CK

            var autoCarOpConfig = new AutoCarOperations.Model.ConfigurationData()
            {
                ConnectionString = ConfigurationData.ConnectionString,
                CKOrderFolder = ConfigurationData.CKOrderFolder,
                MandrilAPIKey = ConfigurationData.MandrilAPIKey,
                FTPAddress = ConfigurationData.FTPAddress,
                FTPUserName = ConfigurationData.FTPUserName,
                FTPPassword = ConfigurationData.FTPPassword,
            };

            OrderDAL.UploadOrderToCK(autoCarOpConfig, true, true, orderList);
            //mark the above orders as Submitted in local table
            OrderDAL.UpdateStatus(ConfigurationData.ConnectionString, orderList);
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
        internal static string FTPAddress = ConfigurationManager.AppSettings["FTPAddress"];
        internal static string FTPUserName = ConfigurationManager.AppSettings["FTPUserName"];
        internal static string FTPPassword = ConfigurationManager.AppSettings["FTPPassword"];
        internal static string Store = ConfigurationManager.AppSettings["Store"];
        internal static string CKOrderFolder = ConfigurationManager.AppSettings["CKOrderFolder"];
        internal static string MandrilAPIKey = ConfigurationManager.AppSettings["MandrilAPIKey"];
    }
}
