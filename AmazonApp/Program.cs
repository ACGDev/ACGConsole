﻿using AmazonApp.Helper;
using AmazonApp.Model;
using AutoCarOperations;
using AutoCarOperations.DAL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using DevDefined.OAuth.Utility;
using Newtonsoft.Json;
using ACG = DCartRestAPIClient;
using System.Globalization;


namespace AmazonApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // SAM: update customer list fist
            //CustomerDAL.AddCustomer(config);

            MarketplaceWebServiceOrdersConfig config = new MarketplaceWebServiceOrdersConfig();
            config.ServiceURL = ConfigurationHelper.ServiceURL;
            
            // Set other client connection configurations here if needed
            // Create the client itself

            MarketplaceWebServiceOrders client = new MarketplaceWebServiceOrdersClient(ConfigurationHelper.AccessKey, ConfigurationHelper.SecretKey, ConfigurationHelper.AppName, ConfigurationHelper.Version, config);
            MarketplaceWebServiceOrdersSample amazonOrders = new MarketplaceWebServiceOrdersSample(client);

            //CreateProduct();

            // Setup the orders service client
            try
            {
                IMWSResponse response = null;
                response = amazonOrders.InvokeListOrders();
                ResponseHeaderMetadata rhmd = response.ResponseHeaderMetadata;
                Console.WriteLine("RequestId: " + rhmd.RequestId);
                Console.WriteLine("Timestamp: " + rhmd.Timestamp);
                var orderResponse = response as ListOrdersResponse;
                var invoiceNo = OrderDAL.GetMaxInvoiceNum(ConfigurationHelper.ConnectionString, "ACGA-");
                
                foreach (var order in orderResponse.ListOrdersResult.Orders)
                {
                    order.BuyerEmail = order.BuyerEmail.Replace("marketplace.amazon.com", "AutoCareGuys.com");
                    if (order.OrderStatus.ToLower() != "unshipped")
                        continue;

                    Console.WriteLine("Checking Unshipped Amazon order :" + order.AmazonOrderId);
                    //** SAM: Find if this order already exists (but unshipped)
                    var existingOrder = OrderDAL.FetchOrders(ConfigurationHelper.ConnectionString, ord => ord.po_no == order.AmazonOrderId);
                    if (existingOrder.Count >0)
                    {
                        Console.WriteLine("  Order exists " + existingOrder[0].orderno);
                        continue;
                    }

                    // Add order
                    var orderItemResponse = amazonOrders.InvokeListOrderItems(order.AmazonOrderId);
                    order.OrderItem = orderItemResponse.ListOrderItemsResult.OrderItems;
                    var customerId = CreateCustomer(order);  
                    if (customerId>0)
                    {
                        var mappedOrder = MapAmazonOrder(order, customerId, ref invoiceNo);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred "+ex.Message);
                Console.ReadKey();
            }
        }

        private static void CreateProduct()
        {
            var config2 = new MarketplaceWebServiceConfig();
            // Set configuration to use US marketplace
            config2.ServiceURL = ConfigurationHelper.ServiceURL;
            // Set the HTTP Header for user agent for the application.
            config2.SetUserAgentHeader(
                ConfigurationHelper.AppName,
                ConfigurationHelper.Version,
                "C#");

            var _AmazonClient = new MarketplaceWebServiceClient(ConfigurationHelper.AccessKey,
                ConfigurationHelper.SecretKey,
                config2);
            Dictionary<string, string> types = new Dictionary<string, string>
            {
                {"Product", "_POST_PRODUCT_DATA_"},
                {"Price", "_POST_PRODUCT_PRICING_DATA_"},
                {"Inventory", "_POST_INVENTORY_AVAILABILITY_DATA_"},
            };
            foreach (var type in types)
            {
                var liObj = GetProducts(type.Key);
                SubmitFeedRequest request = new SubmitFeedRequest
                {
                    Merchant = ConfigurationHelper.SellerId,
                    FeedContent = FeedRequestXML.GenerateInventoryDocument(ConfigurationHelper.AppName, liObj)
                };
                // Calculating the MD5 hash value exhausts the stream, and therefore we must either reset the
                // position, or create another stream for the calculation.
                request.ContentMD5 = MarketplaceWebServiceClient.CalculateContentMD5(request.FeedContent);
                request.FeedContent.Position = 0;

                request.FeedType = type.Value;

                var subResp = FeedSample.InvokeSubmitFeed(_AmazonClient, request);
                request.FeedContent.Close();
                var feedReq = new GetFeedSubmissionResultRequest()
                {
                    Merchant = ConfigurationHelper.SellerId,
                    FeedSubmissionId = subResp.SubmitFeedResult.FeedSubmissionInfo.FeedSubmissionId,//"50148017726",
                    FeedSubmissionResult = File.Open("feedSubmissionResult1.xml", FileMode.OpenOrCreate,
                        FileAccess.ReadWrite)
                };
                Thread.Sleep(10000);
                //need to handle error else the loop will be infinite
                while (true)
                {
                    var getResultResp = FeedSample.InvokeGetFeedSubmissionResult(_AmazonClient, feedReq);
                    if (getResultResp != null)
                    {
                        using (var stream = feedReq.FeedSubmissionResult)
                        {
                            XDocument doc = XDocument.Parse(stream.ReadToEnd()); //or XDocument.Load(path)
                            string jsonText = JsonConvert.SerializeXNode(doc);
                            dynamic dyn = JsonConvert.DeserializeObject<ExpandoObject>(jsonText);
                            dynamic processingSummary = dyn.AmazonEnvelope.Message.ProcessingReport.ProcessingSummary;
                            if (processingSummary.MessagesProcessed == processingSummary.MessagesSuccessful)
                            {
                                break;
                            }
                            //else
                            //{
                            //    //send email with failed sku info
                            //}
                        }
                    }
                }
                File.Delete("feedSubmissionResult1.xml");
            }
        }
        private static List<FeedModel> GetProducts(string type)
        {
            List<FeedModel> liObj = new List<FeedModel>()
            {
                new FeedModel
                {
                    sku = "CSC2S3NS7074",
                    asin = "B000ZARVDE",
                },
                new FeedModel
                {
                    sku = "CSC2S3DG7035",
                    asin = "B000ZARVDO",
                },
                new FeedModel
                {
                    sku = "CSC2S1FD7242",
                    asin = "B000ZARVDY",
                },
                new FeedModel
                {
                    sku = "CSC2S8KI7001",
                    asin = "B000ZARVEI",
                }
            };
            foreach (var obj in liObj)
            {
                obj.type = type;
                switch (type)
                {
                    case "Price":
                        obj.price = 119.99;
                        break;
                    case "Inventory":
                        obj.quantity = 20;
                        obj.fulfillmentLatency = 12;
                        break;
                }
            }
            return liObj;
        }
        public static long CreateCustomer(Order order)
        {
            //check if customer already exist
            var customerOld = CustomerDAL.FindCustomer(ConfigurationHelper.ConnectionString, cus => cus.billing_address == order.ShippingAddress.AddressLine1 && cus.billing_zip == order.ShippingAddress.PostalCode);
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
                ShippingState = order.ShippingAddress.StateOrRegion.Trim(),
                ShippingZipCode = order.ShippingAddress.PostalCode,
                ShippingCountry = order.ShippingAddress.CountryCode.ToUpper(), 
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
            // check for US States
            if ((customer.ShippingCountry == "US" || customer.ShippingCountry == "USA") && customer.ShippingState.Length > 2)
                customer.ShippingState = OrderDAL.GetStateCodeForUS(customer.ShippingState);
            if ((customer.BillingCountry == "US" || customer.BillingCountry == "USA") && customer.BillingState.Length > 2)
                customer.BillingState = OrderDAL.GetStateCodeForUS(customer.BillingState);

            var recordInfo = RestHelper.AddRecord(customer, "Customers", ConfigurationHelper.PrivateKey,
                ConfigurationHelper.Token, ConfigurationHelper.Store);
            if (recordInfo.Status == ACG.ActionStatus.Failed)
            {
                MandrillMail.SendEmail(ConfigurationHelper.MandrilAPIKey, "Order Has to be processed manually. ",
                    "Order Has to be processed manually. Could not create customer. The order no is:" + order.AmazonOrderId,
                    "sales@autocareguys.com");
                return 0;
            }
            var customerId = Convert.ToInt32(recordInfo.ResultSet);
            customer.CustomerID = customerId;
            CustomerDAL.AddCustomers(ConfigurationHelper.ConnectionString, new List<ACG.Customer>() { customer }, new List<ACG.CustomerGroup>());
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
            if (string.IsNullOrEmpty(acgOrder.BillingLastName))
                acgOrder.BillingLastName = acgOrder.BillingFirstName;
            if (string.IsNullOrEmpty(acgOrder.ShipmentList[0].ShipmentLastName))
                acgOrder.ShipmentList[0].ShipmentLastName = acgOrder.ShipmentList[0].ShipmentFirstName;
            foreach (var item in order.OrderItem)
            {

                // ** SAM: Make sure we have AmazonOrderItemID stored in some table for every order item

                // ** SAM: From Amazon, find Manufacturer Part no for this ASIN. Then use that MPN to map our part no.
                // Search our ASIN table, if the needed, insert this row. Or update.
                // SAM: It will be better if we can capture the SKU (new) coming back from Amazon - Can put it in ASIN table.

                // OR - simply map ASIN with our ASIN table and get the part no.
                var orderItem = ProductDAL.FindOrderFromASIN(ConfigurationHelper.ConnectionString, item.ASIN, item.SellerSKU);
                if (orderItem == null || orderItem.catalogid == null
                    || orderItem.ItemId == null )
                {
                    MandrillMail.SendEmail(ConfigurationHelper.MandrilAPIKey, "Order Has to be processed manually. ",
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
            
            
            var orderRes = RestHelper.AddRecord(acgOrder, "Orders", ConfigurationHelper.PrivateKey, ConfigurationHelper.Token,
                ConfigurationHelper.Store);
            if (orderRes.Status == ACG.ActionStatus.Failed)
            {
                MandrillMail.SendEmail(ConfigurationHelper.MandrilAPIKey, "Order Has to be processed manually",
                    "Order Has to be processed manually. The order no is:" + order.AmazonOrderId+ Environment.NewLine +
                    "Error: "+ orderRes.Description,
                    "sales@autocareguys.com");
                return null;
            }

            var orderId = Convert.ToInt32(orderRes.ResultSet);
            acgOrder.OrderID = orderId;
            var orderList = OrderDAL.Map_n_Add_ExtOrders(ConfigurationHelper.ConnectionString, "", new List<ACG.Order>() { acgOrder });
            // SAM: No need to sync orders - just place it to CK
             
            var autoCarOpConfig = new AutoCarOperations.Model.ConfigurationData()
            {
                ConnectionString = ConfigurationHelper.ConnectionString,
                CKOrderFolder = ConfigurationHelper.CKOrderFolder,
                MandrilAPIKey = ConfigurationHelper.MandrilAPIKey,
                FTPAddress = ConfigurationHelper.FTPAddress,
                FTPUserName = ConfigurationHelper.FTPUserName,
                FTPPassword = ConfigurationHelper.FTPPassword,
            };
            if (order.OrderItem[0].ASIN != "B00JFEU78W" && order.OrderItem.Count==1)   // Avoid uploading Drawstring bag orders
            {
                OrderDAL.UploadOrderToCK(autoCarOpConfig, true, true, orderList);
                //mark the above orders as Submitted in local table
            }
            else
            {
                Console.WriteLine(string.Format("Drawstring Bag, M4I98, ASIN B00JFEU78W - handle manually"));
            }
            OrderDAL.UpdateStatus(ConfigurationHelper.ConnectionString, orderList);
            return acgOrder;
        }
    }
}
