using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;
using DCartRestAPIClient;
using MySql.Data.MySqlClient;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace AutoCarOperations.DAL
{
    public static class OrderDAL
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configData"></param>
        /// <param name="fetchDate"></param>
        /// <param name="prepareFile"></param>
        /// <param name="uploadFile"></param>
        public static void PlaceOrder(ConfigurationData configData, bool fetchDate = true, bool prepareFile = true, bool uploadFile = true, List<orders> orderList = null)
        {
            if (orderList == null)
            {
                orderList = SyncOrders(configData, fetchDate);
            }
            if (prepareFile && orderList.Count > 0)
            {
                //returns Filepath, FileName
                var orderListCopy = JsonConvert.DeserializeObject<List<orders>>(JsonConvert.SerializeObject(orderList));
                var fileDetail = PrepareOrderFile(configData, orderList);
                //upload files only if counter > 0
                int counter = orderList.Count(I => I.shipcomplete.ToLower() == "pending");
                if (uploadFile && counter > 0)
                {
                    //need to create donload stream as ref doesnt allow optional parameter
                    //todo: create overloaded method
                    FTPHandler.DownloadOrUploadOrDeleteFile(configData.FTPAddress, configData.FTPUserName, configData.FTPPassword, fileDetail.Item1, fileDetail.Item2, WebRequestMethods.Ftp.UploadFile);
                    //Update Order status as Submitted
                    UpdateStatus(configData.ConnectionString, orderListCopy);
                }
            }
        }
        /// <summary>
        /// Fetches orders from site and syncs with Admin DB
        /// </summary>
        /// <param name="config"></param>
        /// <param name="fetchDate"></param>
        /// <returns></returns>
        private static List<orders> SyncOrders(ConfigurationData config, bool fetchDate)
        {
            var strOrderStart = FetchLastOrderDate(config.ConnectionString, fetchDate);
            List<Order> orders_fromsite = new List<Order>();
            var skip = 0;
            while (true)
            {
                var records = RestHelper.GetRestAPIRecords<Order>("", "Orders", config.PrivateKey, config.Token, config.Store, "100", skip, strOrderStart);
                int counter = records.Count;
                Console.WriteLine("..........Fetches " + counter + " Order Record..........");
                orders_fromsite.AddRange(records);
                if (counter < 100)
                {
                    break;
                }
                skip = 101 + skip;
            }
            var syncedOrders = Map_n_Add_ExtOrders(config.ConnectionString, strOrderStart, orders_fromsite);  // Adds and updates orders from external site
            return syncedOrders;
        }

        /// <summary>
        /// Adds and updates orders from external site
        /// </summary>
        /// <param name="strOrderStart"></param>
        /// <param name="orders_fromsite"></param>
        /// <returns></returns>
        private static List<orders> Map_n_Add_ExtOrders(string connectionString, string strOrderStart, List<Order> orders_fromsite)
        {
            var mappedOrders = MapOrders(orders_fromsite);
            using (var context = new AutoCareDataContext(connectionString))
            {
                bool flag = false;
                foreach (var order_external in mappedOrders)
                {
                    bool bAddthisOrder = false;
                    // -- status: 4 - shipped, 5 - Cancelled, 7 - incomplete, 1 - new
                    if (order_external.order_status == 7)
                    {
                        continue;
                    }
                    var record_from_adminDB = context.Orders.FirstOrDefault(I => I.order_id == order_external.order_id);
                    // Handle if new order or not shipped or cancelled 
                    if (record_from_adminDB == null)
                        bAddthisOrder = true;
                    else if (order_external.order_status == 5 && record_from_adminDB.order_status != 5)  // if previously shipped, it can be cancelled now
                        bAddthisOrder = true;
                    else  if (record_from_adminDB.order_status != 5) // skip if previously cancelled
                        bAddthisOrder = true;

                    if (bAddthisOrder)
                    {
                        if (order_external.referer.Length > 98)
                        {
                            order_external.referer = order_external.referer.Substring(0, 98);
                            Console.WriteLine("referer "+order_external.referer+" length "+order_external.referer.Length.ToString());
                        }
                        if (order_external.po_no.Length > 49)
                        {
                            Console.WriteLine("po_no " + order_external.po_no + " length " + order_external.po_no.Length.ToString());
                            order_external.po_no = order_external.po_no.Substring(0, 49);
                            Console.WriteLine("  po_no truncated to " + order_external.po_no );
                        }
                        if (record_from_adminDB != null && record_from_adminDB.shipcomplete.ToLower() == "submitted" && order_external.status==1)
                            order_external.shipcomplete = record_from_adminDB.shipcomplete;

                        context.Orders.AddOrUpdate(order_external);
                        flag = true;
                    }
                }
                if (flag)
                {
                    context.SaveChanges();
                }
                
                DateTime strOrderDate = Convert.ToDateTime(strOrderStart).AddDays(-5);
                return context.Orders.Include(I => I.order_items).Include(I => I.order_shipments).Include("order_items.Product").Where(I => I.orderdate >= strOrderDate).ToList();
            }
        }
        /// <summary>
        ///     Update Order Status as 'Submitted' in Database
        /// </summary>
        /// <param name="db_orders"></param>
        private static void UpdateStatus(string connectionString, List<orders> db_orders)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                foreach (var order in db_orders)
                {
                    if (order.shipcomplete != "Submitted")
                    {
                        context.Entry(order).State = EntityState.Modified;
                        context.Orders.Attach(entity: order);
                        order.shipcomplete = "Submitted";
                        context.Entry(order).Property(I => I.shipcomplete).IsModified = true;
                    }
                    var sequenceNo = 1;
                    //todo: need to confirm when Product is null
                    foreach (var item in order.order_items)
                    {
                        for (int i = 0; i < item.numitems; i++)
                        {
                            var orderItemDet = new order_item_details
                            {
                                order_item_id = item.order_item_id,
                                order_no = order.orderno,
                                sequence_no = sequenceNo,
                                item_id = item.itemid,
                                sku = item.Product != null ? item.Product.SKU : null,
                                //ship_agent =,
                                //ship_service_code = order.ship
                                //tracking_no = order.order_shipments != null && order.order_shipments.Count > 0 ? order.order_shipments[0].trackingcode : "",
                                //ship_date = order.d,
                            };
                            //todo: if qty > 1
                            sequenceNo = sequenceNo + 1;
                            context.OrderItemDetails.Add(orderItemDet);
                        }
                    }
                }
                context.SaveChanges();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="shipments"></param>
        /// <returns></returns>
        private static List<order_shipments> GetOrderShipments(long? orderId, List<Shipment> shipments)
        {
            List<order_shipments> orderShipments = null;
            if (shipments.Count > 0)
            {
                orderShipments = new List<order_shipments>();
                foreach (var ship in shipments)
                {
                    orderShipments.Add(new order_shipments
                    {
                        last_update = ship.ShipmentLastUpdate,
                        order_status = ship.ShipmentOrderStatus,
                        order_id = orderId,
                        catalogid = 0,
                        oshipmethod = ship.ShipmentMethodName,
                        oshipmethodid = ship.ShipmentMethodID,
                        oshippeddate = ship.ShipmentShippedDate,
                        shipping_id = ship.ShipmentID,
                        trackingcode = ship.ShipmentTrackingCode
                    });
                }
            }
            return orderShipments;
        }
        /// <param name="orderId"></param>
        /// <param name="orderItems"></param>
        /// <returns></returns>
        private static List<order_items> GetOrderItems(long? orderId, List<OrderItem> orderItems)
        {
            List<order_items> order_items = null;
            if (orderItems.Count > 0)
            {
                order_items = new List<order_items>();
                foreach (var item in orderItems)
                {
                    order_items.Add(new order_items
                    {
                        additional_field1 = item.ItemAdditionalField1,
                        additional_field2 = item.ItemAdditionalField2,
                        additional_field3 = item.ItemAdditionalField3,
                        catalogid = item.CatalogID,
                        catalogidoptions = item.ItemCatalogIDOptions,
                        date_added = item.ItemDateAdded,
                        itemdescription = item.ItemDescription,
                        itemid = item.ItemID,
                        numitems = item.ItemQuantity,
                        optionprice = item.ItemOptionPrice,
                        options = item.ItemOptions,
                        order_id = orderId,
                        shipment_id = item.ItemShipmentID,
                        unitcost = item.ItemUnitCost,
                        unitprice = item.ItemUnitPrice,
                        unitstock = item.ItemUnitStock,
                        weight = item.ItemWeight,
                        depends_on_item = 0,
                        itemname = "Fake",
                        recurrent = 0,
                        recurring_order_frequency = 0,
                        supplierid = 0
                    });
                }
            }
            return order_items;
        }
        /// <summary>
        /// Convert (maps) REST Order List from Order site to DB orders
        /// </summary>
        /// <param name="ordersToMap"></param>
        /// <returns></returns>
        public static List<orders> MapOrders(List<Order> ordersToMap)
        {
            List<orders> mappedOrders = new List<orders>();
            foreach (var order in ordersToMap)
            {
                var orderKeyDict = order.ContinueURL?.Split('&').Select(q => q.Split('='))
                                       .ToDictionary(k => k[0], v => v[1]) ?? new Dictionary<string, string>();
                var orderShipMents = order.ShipmentList[0];
                if (order.OrderStatusID == 7)
                    continue;
                var po_number = order.CustomerComments;
                if (po_number.ToUpper().Contains("PO ") && order.BillingEmail == "support@justfeedwebsites.com")
                {
                    po_number = po_number.ToUpper().Replace("PO NO", "").Replace("BUYER: 500","").Trim();
                    po_number = po_number.Replace("BUYER 500", "").Replace("BUYER:500", "");
                    po_number = po_number.Replace(":", "").Replace(";", "").Replace(",", "").Trim();
                }
            
                orders thisOrder = new orders
                {
                    discount = order.OrderDiscount,
                    last_update = order.LastUpdate,
                    affiliate_commission = order.AffiliateCommission,
                    billaddress = order.BillingAddress,
                    billaddress2 = order.BillingAddress2,
                    billcity = order.BillingCity,
                    billcountry = order.BillingCountry,
                    billemail = order.BillingEmail,
                    billfirstname = order.BillingFirstName,
                    billlastname = order.BillingLastName,
                    billphone = order.BillingPhoneNumber,
                    billstate = order.BillingState,
                    billzip = order.BillingZipCode,
                    cus_comment = order.CustomerComments,
                    customerid = order.CustomerID,
                    internalcomment = order.InternalComments,
                    invoicenum = order.InvoiceNumber,
                    invoicenum_prefix = order.InvoiceNumberPrefix,
                    ip = order.IP,
                    last_order = order.LastUpdate,
                    ocompany = order.BillingCompany,
                    order_id = order.OrderID.Value,
                    order_status = order.OrderStatusID,
                    orderamount = order.OrderAmount,
                    orderdate = order.OrderDate,
                    ordertax2 = order.SalesTax2,
                    ordertax3 = order.SalesTax3,
                    referer = order.Referer,
                    parent_orderid = order.OrderID,
                    paymethod = order.BillingPaymentMethod,
                    salesperson = order.SalesPerson,
                    salestax = order.SalesTax,
                    userid = order.UserID,
                    affiliate_approved = 0,
                    affiliate_approvedreason = "",
                    affiliate_id = 0,
                    ccauthorization = "",
                    coupon = "",
                    coupondiscount = 0,
                    coupondiscountdual = 0,
                    date_started = DateTime.Now,
                    errors = "Fake",
                    giftamountused = 0,
                    giftamountuseddual = 0,
                    giftcertificate = "",
                    isrecurrent = 0,
                    last_auto_email = 0,
                    next_order = DateTime.Now,
                    ohandling = 0,
                    orderboxes = 0,
                    orderkey = orderKeyDict.ContainsKey("orderkey") ? orderKeyDict["orderkey"] : null,//can found from ContinueURL
                    orderno = order.InvoiceNumberPrefix.Trim() + order.InvoiceNumber.ToString(),    //SM: inv prefix + inv num - previously "Fake"
                    orderweight = 0,//todo:calculate order from orderitem
                    ostep = "",
                    po_no = po_number, // order.CustomerComments.Replace("PO NO:", "").Replace("; Buyer: 500",""),
                    paymethodinfo = "",   // SM
                    recurrent_frequency = 0,
                    shipaddress = orderShipMents.ShipmentAddress,
                    shipaddress2 = orderShipMents.ShipmentAddress2,
                    shipcity = orderShipMents.ShipmentCity,
                    shipcompany = orderShipMents.ShipmentCompany,
                    shipcomplete = "Pending",
                    shipcost = orderShipMents.ShipmentCost,
                    shipcountry = orderShipMents.ShipmentCountry,
                    shipemail = orderShipMents.ShipmentEmail,
                    shipfirstname = orderShipMents.ShipmentFirstName,
                    shiplastname = orderShipMents.ShipmentLastName,
                    shipmethodid = orderShipMents.ShipmentMethodID,
                    shipphone = orderShipMents.ShipmentPhone,
                    shipstate = orderShipMents.ShipmentState,
                    shipzip = orderShipMents.ShipmentZipCode,
                    status = (int)order.OrderStatusID,   // SM
                    order_items = GetOrderItems(order.OrderID, order.OrderItemList),
                    order_shipments = GetOrderShipments(order.OrderID, order.ShipmentList)
                };
                // SM: check for shipment status in detailed records
                if (order.OrderStatusID==5)  // cancelled
                    thisOrder.shipcomplete = "Cancelled";
                else
                {
                    if (order.OrderStatusID == 4)  // cancelled
                        thisOrder.shipcomplete = "Shipped";

                    //int nItemsShipped = 0;
                    //foreach (var shiprow in thisOrder.order_shipments)
                    //{
                    //    if (shiprow.oshippeddate != "")
                    //        nItemsShipped++;
                    //}
                    //if (nItemsShipped == thisOrder.order_shipments.Count)
                    //    thisOrder.shipcomplete = "Shipped";
                    //else if (nItemsShipped > 0)
                    //    thisOrder.shipcomplete = string.Format("Partial {0}/{1}", nItemsShipped, thisOrder.order_items.Count);
                }


                mappedOrders.Add(thisOrder);
            }
            return mappedOrders;
        }
        /// <summary>
        ///     To fetch ALL orders comment pass <para>fetchLastOrderDate</para> false
        /// </summary>
        /// <param name="myConnectionString"></param>
        /// <param name="fetchDate"></param>
        /// <returns></returns>
        private static string FetchLastOrderDate(string myConnectionString, bool fetchDate)
        {
            string strOrderStart = DateTime.Today.AddDays(CommonConstant.NumOfDays).ToString("MM/dd/yyyy");
            if (!fetchDate)
            {
                return strOrderStart;
            }
            MySqlConnection conn;
            try
            {
                using (conn = new MySqlConnection(myConnectionString))
                {

                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText =
                        "select orderdate from orders where Shipcomplete not in ('Pending', 'Cancelled', 'Shipped') order by orderdate limit 1";
                    //Command to get query needed value from DataBase
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var result = reader.GetDateTime("orderdate");
                        strOrderStart = result.ToString("MM/dd/yyyy");

                    }
                    conn.Close();
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Console.WriteLine("Error " + ex.Message);
            }
            return strOrderStart;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        /// <param name="configData"></param>
        /// <param name="sendEmail"></param>
        /// <returns></returns>
        private static string GenerateOrderLines(orders order, ConfigurationData configData, Action<string, string, string, string> sendEmail)
        {
            StringBuilder orderFinal = new StringBuilder("");
            string strMasterPakCode = "";
            string strMasterPakCodeMsg = "";
            if (order.order_items.Count > 1)
            {
                strMasterPakCode = "MASTERPACK";
                strMasterPakCodeMsg = "Please Masterpack all items";
            }
            foreach (var o in order.order_items)
            {
                if (o.shipment_id > 0 || o.itemid == "111111")
                {
                    continue;
                }
                if (o.Product == null)
                {
                    // SM: orderItems not instantiated. Not clear about the logic
                    //orderItems.Add(o);
                    // SM Added - Use product mfg ID same as ItemID, take out CK_
                    o.Product = new products();
                    o.Product.mfgid = o.itemid;
                    if (o.itemid.StartsWith("CK_"))
                        o.Product.mfgid = o.itemid.Replace("CK_", "");
                    //send email
                    sendEmail(configData.MandrilAPIKey, "Missing Products", JsonConvert.SerializeObject(o), "cs@autocareguys.com");
                    //continue;
                }

                var orderDescs = o.itemdescription.Split(new[] { ' ', '\r', '\n', ':', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(I => I.Trim());
                var variant = string.Empty;
                foreach (var desc in orderDescs)
                {
                    if (desc.ToLower().StartsWith("universal"))
                    {
                        variant = "universal";
                        break;
                    }
                    if (!desc.Contains(o.Product.mfgid))
                    {
                        continue;
                    }
                    variant = GetVariant(o.Product.mfgid, desc);
                    break;
                }

                // SM did not understand this logic. commented. orderItems is not instantiated - throwing exception
                //if (variant == string.Empty)
                //{
                //    orderItems.Add(o);
                //    continue;
                //}
                if (variant == "universal")
                {
                    variant = string.Empty;
                }
                order.shipcompany = order.shipcompany.Trim();
                if (order.shipcompany.Length > 25)
                    order.shipcompany = order.shipcompany.Substring(0, 25).Trim();

                //if (order.shipcompany.Length > 25)
                //    order.shipcompany = order.shipcompany.Substring(0, 25).Trim();

                // CK_SKU should be blank when Mfg ID and Variant are present.

                // ** NEED to incorporate length for all fields. See Progarm.cs, line 240

                //"PO,PO_Date,Ship_Company,Ship_Name,Ship_Addr,Ship_Addr_2,Ship_City,Ship_State,Ship_Zip,Ship_Country,

                string oText = string.Format("{0},{1},{2}", order.orderno, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
                    order.shipcompany.Replace("\"", "&quot;"));
                oText += string.Format(",{0}", order.shipfirstname.Trim() + " " + order.shiplastname.Trim());
                oText += string.Format(",{0}", order.shipaddress.Trim().Replace("\"", "&quot;"));
                oText += string.Format(",{0}", order.shipaddress2.Trim().Replace("\"", "&quot;"));
                // MUST have state. If not, set it same as Country
                if (order.shipstate.Trim() == string.Empty)
                    order.shipstate = order.shipcountry;
                // MUST have zip. If not, put 0
                if (order.shipzip.Trim() == string.Empty)
                    order.shipzip = "0";

                oText += string.Format(",{0},{1},{2},{3}",
                    order.shipcity.Trim(), order.shipstate.Trim(), order.shipzip.Trim(), order.shipcountry.Trim());

                // Ship_Phone,Ship_Email,Ship_Service,CK_SKU
                oText += string.Format(",{0},{1},{2},{3}", order.shipphone.Trim(), order.shipemail.Trim(), "R02", "");

                if (!string.IsNullOrEmpty(order.internalcomment))
                {
                    List<string> custom = new List<string>
                    {
                        "CK_Item",
                        "CK_Variant",
                        "Customized_Code",
                        "Customized_Msg",
                        "Customized_Code2",
                        "Customized_Msg2",
                        "Qty",
                        "Comment"
                    };
                    string[] values = new string[custom.Count];
                    string[] splitComment = order.internalcomment.Split(new string[] {Environment.NewLine},
                        StringSplitOptions.RemoveEmptyEntries);
                    //CK_Item
                    values[0] = o.itemid;
                    //CK_Variant
                    values[1] = o.catalogid.ToString();
                    //Qty
                    values[6] = o.numitems.ToString();
                    foreach (var field in splitComment)
                    {
                        string[] splitData = field.Split(':').Select(I => I.Trim()).ToArray();
                        var index = custom.FindIndex(I => I == splitData[0]);
                        values[index] = splitData[1];
                    }
                    //comment
                    values[7] = order.cus_comment + (string.IsNullOrEmpty(values[5]) ? "" : " " + values[5]);
                    oText += string.Join(",", values);
                }
                else
                {
                    // CK_Item,CK_Variant,Customized_Code,Customized_Msg,Customized_Code2,Customized_Msg2,Qty,Comment";
                    oText += string.Format(",{0},{1},{2},{3},{4},{5},{6},{7}", o.Product.mfgid, variant, strMasterPakCode, strMasterPakCodeMsg, "", "",
                        o.numitems, order.cus_comment.Trim().Replace("\"", "&quot;"));
                }
                orderFinal.AppendLine(oText);
                o.Product = null;
            }
            return orderFinal.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mfgId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string GetVariant(string mfgId, string value)
        {
            var variant = "";
            var orderDescs = value.Split(new[] { ' ', ':', ';', '<' }, StringSplitOptions.RemoveEmptyEntries).Select(I => I.Trim());
            foreach (var desc in orderDescs)
            {
                if (desc.StartsWith(mfgId))
                {
                    variant = desc.Replace(mfgId, "");
                }
                if (desc.StartsWith("(" + mfgId))
                {
                    variant = desc.TrimStart('(').TrimEnd(')').Replace(mfgId, "");
                }
            }
            if (!string.IsNullOrEmpty( variant ) && variant.Substring(0, 1) == "-")
                variant = variant.Substring(1);
            return variant;
        }

        private static Tuple<string, string> PrepareOrderFile(ConfigurationData configData, List<orders> orders)
        {
            string fileName = string.Format("ACG-{0}.csv", DateTime.Now.ToString("yyyyMMMdd-HHmm"));
            string filePath =
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string strFileNameWithPath = string.Format("{0}\\{1}", filePath,
                fileName);

            string strCsvHeader = "PO, PO_Date,Ship_Company,Ship_Name,Ship_Addr,Ship_Addr_2,Ship_City,Ship_State,Ship_Zip,Ship_Country,Ship_Phone,Ship_Email,Ship_Service,CK_SKU,CK_Item,CK_Variant,Customized_Code,Customized_Msg,Customized_Code2,Customized_Msg2,Qty,Comment";
            File.WriteAllText(strFileNameWithPath, strCsvHeader + "\r\n");

            foreach (var order in orders)
            {
                if (order.cus_comment !=null && order.cus_comment.ToLower().Contains("(special order)"))
                {
                    MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Has to be processed manually", "Order Has to be processed manually. The order no is:" + order.orderno, "sales@autocareguys.com");
                    continue;
                }
                // manually modify order if needed
                //if (order.orderno.Contains("161968"))
                //    order.orderno += "-6";
                // RestHelper.Execute(@"http://api.coverking.com/orders/Order_Placement.asmx?op=Place_Orders", config.AuthUserName, config.AuthPassowrd, order);
                if (order.shipcomplete.ToLower() == "pending")
                {
                    string strOrderLines = GenerateOrderLines(order, configData, MandrillMail.SendEmail);
                    if (strOrderLines != string.Empty)
                    {
                        File.AppendAllText(strFileNameWithPath, strOrderLines);
                    }
                }
            }
            return Tuple.Create(filePath, fileName);
        }

        public static orders GetOrder(string connectionString, Func<orders, bool> condFunc)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.Orders.Include(I => I.order_items).Include(I => I.order_shipments)
                    .FirstOrDefault(condFunc);
            }
        }

        public static int? GetMaxInvoiceNum(string connectionString, string invoicePrefix)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.Orders.Where(I => I.invoicenum_prefix == invoicePrefix).Max(I => I.invoicenum);
            }
        }

        public static List<orders> FetchOrders(string connectionString, Func<orders, bool> whereFunc)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.Orders.Where(whereFunc).ToList();
            }
        }

        public static void UpdateOrderDetail(string connectionString, 
            string orderNo, string serialNo, string status, string shipAgent,
            string shipServiceCode, string trackingNo, string trackingLink)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                var order_det = context.OrderItemDetails.FirstOrDefault(I => I.order_no == orderNo);
                if (order_det != null)
                {
                    order_det.production_slno = serialNo;
                    order_det.status = status;
                    order_det.status_datetime = DateTime.Now;
                    order_det.ship_agent = shipAgent;
                    order_det.ship_service_code = shipServiceCode;
                    order_det.tracking_no = trackingNo;
                    order_det.tracking_link = trackingLink;
                    context.OrderItemDetails.AddOrUpdate(order_det);
                    context.SaveChanges();
                }
            }
        }
    }
}