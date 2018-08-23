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
        /// <param name="orderList"></param>
        /// <param name="numDaysToSync"></param>
        //SM: Oct  ReWritten to upload one order at a time - to avoid holding up all orders for problem in a specific one
        public static void PlaceOrder(ConfigurationData configData, bool fetchDate = true, bool prepareFile = true, bool uploadFile = true, List<orders> orderList = null, int numDaysToSync = 2)
        {
            if (orderList == null)
            {
                orderList = SyncOrders(configData, fetchDate, numDaysToSync); // orderlist now contains ONLY new orders
                Console.WriteLine("  Sync Order complete. ");
            }
            UploadOrderToCK(configData, prepareFile, uploadFile, orderList);
        }

        public static void UploadOrderToCK(ConfigurationData configData, bool prepareFile, bool uploadFile, List<orders> orderList)
        {
            if (prepareFile && orderList.Count > 0)
            {
                Console.WriteLine($"\r\nNumber of New Orders to be processed: {orderList.Count}");
                //SM: Why do we need orderList as well as orderListCopy when we are not really changing the original orderList ?
                var orderListCopy = JsonConvert.DeserializeObject<List<orders>>(JsonConvert.SerializeObject(orderList));

                string filePath =
                    $"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}\\{configData.CKOrderFolder}";
                string strCsvHeader =
                    "PO,PO_Date,Ship_Company,Ship_Name,Ship_Addr,Ship_Addr_2,Ship_City,Ship_State,Ship_Zip,Ship_Country,Ship_Phone,Ship_Email,Ship_Service,CK_SKU,CK_Item,CK_Variant,Customized_Code,Customized_Msg,Customized_Code2,Customized_Msg2,Qty,Comment";
                // todo: SAM**: If AdditionalField1 exists in order_items then use that as Customized_Code (right after CK_Variant)
                if (uploadFile)
                {
                    foreach (var order in orderList)
                    {
                        if (order.shipcomplete.ToLower() == "pending" && order.order_status == 1)
                        {
                            // Check for Special Order - Do not upload - send email
                            if (order.cus_comment != null && order.cus_comment.ToLower().Contains("(special)"))
                            {
                                Console.WriteLine(
                                    $"  Order Has to be processed manually: {order.orderno}. Sending email to sales");
                                MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Has to be processed manually",
                                    "Order Has to be processed manually. The order no is:" + order.orderno,
                                    "sales@autocareguys.com");
                                continue;
                            }
                            string strOrderLines = GenerateOrderLines(order, configData, MandrillMail.SendEmail);
                            if (strOrderLines != string.Empty)
                            {
                                string fileName = $"{order.orderno}-{DateTime.Now:yyyyMMMdd-HHmm}.csv";
                                string strFileNameWithPath = $"{filePath}\\{fileName}";
                                Console.WriteLine(
                                    $"  Creating Order File in folder {filePath},\r\n    Filename: {fileName}");
                                File.WriteAllText(strFileNameWithPath, strCsvHeader + "\r\n");
                                File.AppendAllText(strFileNameWithPath, strOrderLines);
                                Console.WriteLine(strOrderLines);
                                // If Masterpack - Do not upload - instead email to Van
                                if (strOrderLines.Contains("MASTERPACK"))
                                {
                                    Console.WriteLine(
                                    $"  Order Has to be processed manually: {order.orderno}. Sending email to Van and Sales");
                                    MandrillMail.SendEmail(configData.MandrilAPIKey, "Please process this PO manually",
                                        strOrderLines,
                                        "sales@autocareguys.com");
                                }
                                else
                                {
                                    Console.WriteLine($"  Uplaoding {fileName} to CK Order ftp site");
                                    FTPHandler.DownloadOrUploadOrDeleteFile(configData.FTPAddress, configData.FTPUserName,
                                        configData.FTPPassword, filePath, fileName, WebRequestMethods.Ftp.UploadFile);
                                    Console.WriteLine("  ... file successfully uploaded");
                                }


                            }
                        }
                    }
                    //Update Order status as Submitted
                    Console.WriteLine("  UpdateStatus: Marking orders as Submitted");
                    UpdateStatus(configData.ConnectionString, orderListCopy);
                    Console.WriteLine("  UpdateStatus complete");
                }
            }
        }

        /// <summary>
        /// Fetches orders from site and syncs with Admin DB
        /// </summary>
        /// <param name="config"></param>
        /// <param name="fetchDate"></param>
        /// <param name="numDaysToSync"></param>
        /// <returns></returns>
        private static List<orders> SyncOrders(ConfigurationData config, bool fetchDate, int numDaysToSync = 20)
        {
            int nLastInvoice = 0;
            var strOrderStart = FetchLastOrderDate(config.ConnectionString, fetchDate, ref nLastInvoice, numDaysToSync);
            Console.WriteLine(string.Format("  Syncing orders from 3DCart starting {0}", strOrderStart));
            List<Order> orders_fromsite = new List<Order>();
            var skip = 0;
            while (true)
            {
                var records = RestHelper.GetRestAPIRecords<Order>("", "Orders", config.PrivateKey, config.Token, config.Store, "100", skip, strOrderStart, nLastInvoice);
                int counter = records.Count;
                Console.WriteLine("..........Fetched " + counter + " Order Record..........");
                orders_fromsite.AddRange(records);
                if (counter < 100)
                {
                    break;
                }
                skip = 101 + skip;
            }
            var syncedOrders = Map_n_Add_ExtOrders(config.ConnectionString, strOrderStart, orders_fromsite.Where(i => i.OrderStatusID != 7 && i.InvoiceNumber>= nLastInvoice).ToList());  // Adds and updates orders from external site

            return syncedOrders.Where(I => I.shipcomplete.ToLower() == "pending" && I.order_status == 1).ToList();

        }

        /// <summary>
        /// Adds and updates orders from external site
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="strOrderStart"></param>
        /// <param name="orders_fromsite"></param>
        /// <returns></returns>
        public static List<orders> Map_n_Add_ExtOrders(string connectionString, string strOrderStart, List<Order> orders_fromsite)
        {
            var mappedOrders = MapOrders(orders_fromsite, connectionString);
            Console.WriteLine(string.Format("  Total {0} orders to be Added or Updated", mappedOrders.Count));
            using (var context = new AutoCareDataContext(connectionString))
            {
                bool flag = false;
                foreach (var order_external in mappedOrders)
                {
                    bool bAddthisOrder = false;
                    // -- status: 4 - shipped, 5 - Cancelled, 7 - incomplete, 1 - new
                    //if (order_external.order_status == 7)
                    //{
                    //    continue;
                    //}

                    var record_from_adminDB = context.Orders.FirstOrDefault(I => I.orderno == order_external.orderno);
                    // Handle if new order or not shipped or cancelled 
                    if (record_from_adminDB == null)
                        bAddthisOrder = true;
                    else if (order_external.order_status == 5 && record_from_adminDB.order_status != 5)  // if previously shipped, it can be cancelled now
                        bAddthisOrder = true;
                    else if (record_from_adminDB.order_status != 5) // skip if previously cancelled
                        bAddthisOrder = true;

                    if (bAddthisOrder)
                    {
                        if (order_external.referer.Length > 98)
                        {
                            order_external.referer = order_external.referer.Substring(0, 98);
                            Console.WriteLine("referer " + order_external.referer + " length " + order_external.referer.Length.ToString());
                        }
                        if (order_external.po_no.Length > 49)
                        {
                            Console.WriteLine("po_no " + order_external.po_no + " length " + order_external.po_no.Length.ToString());
                            order_external.po_no = order_external.po_no.Substring(0, 49);
                            Console.WriteLine("  po_no truncated to " + order_external.po_no);
                        }
                        if (record_from_adminDB != null && record_from_adminDB.shipcomplete.ToLower() == "submitted" && order_external.order_status == 1)
                            order_external.shipcomplete = record_from_adminDB.shipcomplete;

                        context.Orders.AddOrUpdate(order_external);
                        flag = true;
                    }
                }
                if (flag)
                {
                    context.SaveChanges();
                }
                foreach (var order_external in mappedOrders)
                {
                    order_external.order_items = context.OrderItems.Include(i => i.Product).Where(i => i.order_id == order_external.order_id).ToList();
                    order_external.order_shipments = context.OrderShipments.Where(i => i.order_id == order_external.order_id).ToList();
                }

                return mappedOrders;
            }
        }
        /// <summary>
        ///     Update Order Status as 'Submitted' in Database
        /// </summary>
        /// <param name="db_orders"></param>
        public static void UpdateStatus(string connectionString, List<orders> db_orders, string shipStatus = "Submitted", int status = 1)
        {

            /**SAM Aug 5, 2018 - Does not update shipped record. Rahul to check **/
            /**
            using (var context = new AutoCareDataContext(connectionString))
            {
                foreach (var order in db_orders)
                {
                    if (order.shipcomplete == shipStatus && order.order_status == status)
                        continue;

                    var currentorder = JsonConvert.DeserializeObject<orders>(JsonConvert.SerializeObject(order));
                    currentorder.order_items = null;
                    currentorder.order_shipments = null;
                    context.Entry(currentorder).State = EntityState.Modified;
                    context.Orders.Attach(entity: currentorder);
                    currentorder.shipcomplete = shipStatus;
                    currentorder.status = status;  //SM
                    currentorder.order_status = status;  // SM
                    context.Entry(currentorder).Property(I => I.shipcomplete).IsModified = true;
                    context.Orders.AddOrUpdate(currentorder);
                    
                }
                context.SaveChanges();
            }
            **/

            
            StringBuilder sb = new StringBuilder();
            foreach (var order in db_orders)
            {
                if (order.shipcomplete == shipStatus && order.order_status == status)
                    continue;
                sb.AppendFormat(
                    "update orders set shipcomplete='{0}', status={1}, order_status={1} where orderno='{2}';",
                    shipStatus, status, order.orderno);
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sb.ToString();
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
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
                        supplierid = 0,
                        channel_order_itemid = item.ChannelOrderItemId
                    });
                }
            }
            return order_items;
        }

        /// <summary>
        /// Convert (maps) REST Order List from Order site to DB orders
        /// </summary>
        /// <param name="ordersToMap"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static List<orders> MapOrders(List<Order> ordersToMap, string connectionString)
        {
            List<orders> mappedOrders = new List<orders>();
            foreach (var order in ordersToMap)
            {
                string thisOrderNoFrom3DCart = order.InvoiceNumberPrefix.Trim() + order.InvoiceNumber.ToString();
                orders thisLocalOrder;
                bool bIsAmazonOrder = false;
                using (var context = new AutoCareDataContext(connectionString))
                {
                    thisLocalOrder = context.Orders.FirstOrDefault(i => i.orderno == thisOrderNoFrom3DCart);
                    if (thisLocalOrder != null)
                    {
                        if (thisLocalOrder.order_status == order.OrderStatusID)
                        {
                            // todo: SAM**: Ignore new orders from Amazon, if any. Also see Amazon App fn MapAmazonOrder for this comment
                            if (!(thisLocalOrder.order_status == 1 && thisLocalOrder.shipcomplete.ToLower() == "pending"))
                                continue;
                        }
                        //Detect AmazonOrder 
                        if (order.Referer.Contains("Amazon"))
                        {
                            bIsAmazonOrder = true;
                        }
                    }
                }

                orders thisOrder = new orders();
                //TODO: Sam Jul 23: If Amazon order, read from Amazon to see if this order has shipped. 
                // If so, update detail records info, order status and shipcomplete values 
                if (!bIsAmazonOrder)
                {
                    Console.WriteLine(
                        thisLocalOrder == null
                            ? $" 3D Cart Order {order.InvoiceNumberPrefix}-{order.InvoiceNumber} will be added "
                            : $" 3D Cart Order {order.InvoiceNumberPrefix}-{order.InvoiceNumber} will be updated ");

                    var orderKeyDict = order.ContinueURL?.Split('&').Select(q => q.Split('='))
                                           .ToDictionary(k => k[0], v => v[1]) ?? new Dictionary<string, string>();
                    var orderShipMents = order.ShipmentList[0];

                    var po_number = order.CustomerComments;
                    //if (po_number != null && po_number.ToUpper().Contains("PO ") && order.BillingEmail == "support@justfeedwebsites.com")
                    //{
                    //    po_number = po_number.ToUpper().Replace("PO NO", "").Replace("BUYER: 500", "").Trim();
                    //    po_number = po_number.Replace("BUYER 500", "").Replace("BUYER:500", "");
                    //    po_number = po_number.Replace(":", "").Replace(";", "").Replace(",", "").Trim();
                    //}

                    thisOrder = new orders
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
                        po_no = order.PONo ?? po_number, // order.CustomerComments.Replace("PO NO:", "").Replace("; Buyer: 500",""),
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
                }
                // SM: check for shipment status in detailed records
                if (order.OrderStatusID == 5)  // cancelled
                    thisOrder.shipcomplete = "Cancelled";
                else
                {
                    if (order.OrderStatusID == 4)  // shipped
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
        private static string FetchLastOrderDate(string myConnectionString, bool fetchDate, ref int nLastInvoice, int numDaysToSync = 2)
        {
            MySqlConnection conn;
            string strOrderStart = DateTime.Today.AddDays(-numDaysToSync).ToString("MM/dd/yyyy");
            string strStartForAPI = DateTime.Today.AddDays(-numDaysToSync).ToString("yyyy-MM-dd hh:mm:ss");
            if (!fetchDate)
            {
                // added by Sam as Datewise retreival of records are not working.
                using (conn = new MySqlConnection(myConnectionString))
                {

                    MySqlCommand cmd = conn.CreateCommand();
                    // 4: Shipped, 5: Cancelled
                    cmd.CommandText =
                        String.Format("select invoicenum from orders where order_status not in (4, 5) and orderdate<='{0}' order by invoicenum desc limit 1", strStartForAPI);
                    //Command to get query needed value from DataBase
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        nLastInvoice = reader.GetInt32("invoicenum");
                    }
                    conn.Close();
                }
                return strOrderStart;
            }
            
            try
            {
                using (conn = new MySqlConnection(myConnectionString))
                {

                    MySqlCommand cmd = conn.CreateCommand();
                    // 4: Shipped, 5: Cancelled
                    cmd.CommandText =
                        "select orderdate, invoicenum from orders where order_status not in (4, 5) order by orderdate limit 1";
                    //Command to get query needed value from DataBase
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var result = reader.GetDateTime("orderdate");
                        strOrderStart = result.ToString("MM/dd/yyyy");
                        nLastInvoice = reader.GetInt32("invoicenum");

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
        // Fethces total order value (shipped plus not) from a certain date for a specific billing address 
        public static double FetchTotalOrderAmt(string myConnectionString, string strDate, String custEmail )
        {
            Double totalAmt = 0.0;
            DateTime startDate;
            if (strDate == "")
                startDate = DateTime.Today;
            else
                startDate = Convert.ToDateTime(strDate);

            MySqlConnection conn;
            try
            {
                using (conn = new MySqlConnection(myConnectionString))
                {

                    MySqlCommand cmd = conn.CreateCommand();
                    // 4: Shipped, 5: Cancelled
                    cmd.CommandText =
                        String.Format(
                            "select sum(orderamount) as TotalAmount from orders where billemail = '{0}' and order_status in (1, 4) and "+
                            " po_no in (select PO from jfw_orders where PO_Date>='{1}');",
                            custEmail, startDate.ToString("yyyy-mm-dd"));

                    
                    //Command to get query needed value from DataBase
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        totalAmt = reader.GetDouble("TotalAmount");
                    }
                    conn.Close();
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Console.WriteLine("Error " + ex.Message);
            }
            return totalAmt;
        }
        // SM: trims a string to given length, as well as checks for comma and replaces by ; and removes ' and "
        private static string TrimTolength(string inputStr, int maxLength)
        {
            string outputStr = inputStr.Trim();
            if (outputStr.Length > maxLength)
                outputStr = outputStr.Substring(0, maxLength - 1).Trim();
            outputStr = outputStr.Replace(",", ";").Replace("'", "");
            outputStr = outputStr.Replace("\"", "");

            return outputStr;
        }
        // Gets 2 char state codes from long ones for US
        public static string GetStateCodeForUS(string inState)
        {
            TextInfo tiState = new CultureInfo("en-US", false).TextInfo;
            string stateTitleCase = tiState.ToTitleCase(inState.ToLower());
            string[,] arrState = new string[,]
            {
                {"Alabama", "AL"},
                {"Alaska", "AK"},
                {"Arizona", "AZ"},
                {"Arkansas", "AR"},
                {"Armed Forces America", "AA"},
                {"Armed Forces Europe", "AE"},
                {"Armed Forces Pacific", "AP"},
                {"California", "CA"},
                {"Colorado", "CO"},
                {"Connecticut", "CT"},
                {"Delaware", "DE"},
                {"District of Columbia", "DC"},
                {"Florida", "FL"},
                {"Georgia", "GA"},
                {"Hawaii", "HI"},
                {"Idaho", "ID"},
                {"Illinois", "IL"},
                {"Indiana", "IN"},
                {"Iowa", "IA"},
                {"Kansas", "KS"},
                {"Kentucky", "KY"},
                {"Louisiana", "LA"},
                {"Maine", "ME"},
                {"Maryland", "MD"},
                {"Massachusetts", "MA"},
                {"Michigan", "MI"},
                {"Minnesota", "MN"},
                {"Mississippi", "MS"},
                {"Missouri", "MO"},
                {"Montana", "MT"},
                {"Nebraska", "NE"},
                {"Nevada", "NV"},
                {"New Hampshire", "NH"},
                {"New Jersey", "NJ"},
                {"New Mexico", "NM"},
                {"New York", "NY"},
                {"North Carolina", "NC"},
                {"North Dakota", "ND"},
                {"Ohio", "OH"},
                {"Oklahoma", "OK"},
                {"Oregon", "OR"},
                {"Pennsylvania", "PA"},
                {"Rhode Island", "RI"},
                {"South Carolina", "SC"},
                {"South Dakota", "SD"},
                {"Tennessee", "TN"},
                {"Texas", "TX"},
                {"Utah", "UT"},
                {"Vermont", "VT"},
                {"Virginia", "VA"},
                {"Washington", "WA"},
                {"West Virginia", "WV"},
                {"Wisconsin", "WI"},
                {"Wyoming", "WY"}
            };
            string outState = inState;
            for (int i = 0; i < arrState.Length / 2; i++)
            {
                if (arrState[i, 0] == stateTitleCase)
                {
                    outState = arrState[i, 1];
                }
            }
            return outState;  // if no match - just return the original state
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

            // SM: Oct 21, '17: IGNORE customer comments for now.
            string cus_comment = ""; // order.cus_comment

            string strMasterPakCode = "";
            string strMasterPakCodeMsg = "";
            if (String.IsNullOrEmpty(order.shipcompany))
            {
                order.shipcompany = Convert.ToString(order.ocompany);
                if (String.IsNullOrEmpty(order.shipcompany))
                    order.shipcompany = "";                
            }


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
                    if (o.itemid.StartsWith("SK_"))
                        o.Product.mfgid = o.itemid.Replace("SK_", "");
                    //send email
                    sendEmail(configData.MandrilAPIKey, "Product is null in GenerateOrderLines", JsonConvert.SerializeObject(o), "sam@autocareguys.com");
                    //continue;
                }

                // SAM: Try to find Variant from description ONLY if it is not present in incoming data
                var variant = string.Empty;
                if (! String.IsNullOrEmpty(o.options))
                {
                    variant = o.options ;
                }
                else
                {
                    // SAM: Try to get Variant from description
                    var orderDescs = o.itemdescription.Split(new[] { ' ', '\r', '\n', ':', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(I => I.Trim());

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

                    if (variant == "universal")
                    {
                        variant = string.Empty;
                    }

                }
                
                order.shipcompany = Convert.ToString(order.shipcompany);
                order.shipcompany = order.shipcompany.Trim();
                if (order.shipcompany.Length > 25)
                    order.shipcompany = order.shipcompany.Substring(0, 25).Trim();                    
                

                // CK_SKU should be blank when Mfg ID and Variant are present.

                //"PO,PO_Date,Ship_Company,Ship_Name,Ship_Addr,Ship_Addr_2,Ship_City,Ship_State,Ship_Zip,Ship_Country,

                /** SM: Oct 21, 17: Need to check field lengths for uploading orders to CK **/
                /*
                Ship_Company	The ship to company address ( if applicable )			25 characters max
                Ship_Name	Ship To Person's name			25 characters max
                Ship_Addr	Ship To Address ( limit 30 Charecters )			30 characters max
                Ship_Addr_2	Ship To Address ( limit 30 Charecters )			30 characters max
                Ship_City	Ship To City			20 characters max
                Ship_State	Ship To State			10 characters max
                Ship_Zip	Ship To Zip Code			10 characters max
                Ship_Country	Ship To Country ( Please use the UN 2 digit country codes )			
                Ship_Phone	Ship To Phone Number			15 characters max
                Ship_Email	Ship To Email Address			25 characters max
                ...
                Customized_Code	Customization codes for Logo's or Embroidery			
                Customized_Msg	The Embroidery message that will go with the previous column. Limit 15 charecters			
                Customized_Code2	Second Logo or Embroidery for the item			
                Customized_Msg2	Second Embroidery Message that will go the code			
                Qty	Quantity for this item			
                Comment	Notes on this order item.			35 characters max

                    */

                string oText =
                    $"{order.orderno},{DateTime.Now:MM/dd/yyyy HH:mm:ss},{order.shipcompany.Replace("\"", "&quot;")}";
                oText += $",{TrimTolength(order.shipfirstname.Trim() + " " + order.shiplastname, 25)}";
                oText += $",{TrimTolength(order.shipaddress, 30)}";
                oText += $",{TrimTolength(order.shipaddress2, 30)}";
                // MUST have state. If not, set it same as Country
                if (order.shipstate.Trim() == string.Empty)
                    order.shipstate = order.shipcountry;
                // MUST have zip. If not, put 0
                if (order.shipzip.Trim() == string.Empty)
                    order.shipzip = "0";

                if ((order.shipcountry == "US" || order.shipcountry == "USA") && order.shipstate.Length > 2)
                    order.shipstate = GetStateCodeForUS(order.shipstate);

                oText +=
                    $",{TrimTolength(order.shipcity, 20)},{TrimTolength(order.shipstate, 10)},{TrimTolength(order.shipzip, 10)},{TrimTolength(order.shipcountry, 2)}";

                //SAM: June 25, 18:: Avoid sending autocareguys.com pseudo email addresses
                string shipEmail = TrimTolength(order.shipemail, 25);
                if (order.shipemail.EndsWith("AutoCareGuys.com"))
                    shipEmail = "";

                // Ship_Phone,Ship_Email,Ship_Service,CK_SKU  *** Aug 20, 18: reinstated CDC with WC: 
                if (o.Product.mfgid.StartsWith("CSS") || o.Product.mfgid.StartsWith("CDC") )
                    oText += $",{TrimTolength(order.shipphone, 15)},{shipEmail},WC,";
                else
                    oText += $",{TrimTolength(order.shipphone, 15)},{shipEmail},R02,";

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
                    string[] splitComment = order.internalcomment.Split(new string[] { Environment.NewLine },
                        StringSplitOptions.RemoveEmptyEntries);
                    //CK_Item
                    values[0] = o.Product.mfgid;
                    //CK_Variant
                    values[1] = variant;
                    // Resource Code, if present
                    values[2] = Convert.ToString(o.additional_field1);
                    //Qty
                    values[6] = o.numitems.ToString();
                    // handle multiple Qty
                    if (o.numitems>1)
                    {
                        strMasterPakCode = "MASTERPACK";
                        strMasterPakCodeMsg = "Please Masterpack all items";
                    }
                    

                    if (order.internalcomment != "fake")   // Needed for Amazon orders
                    {
                        foreach (var field in splitComment)
                        {
                            string[] splitData = field.Split(':').Select(I => I.Trim()).ToArray();
                            var index = custom.FindIndex(I => I == splitData[0]);
                            if (index >= 0) values[index] = splitData[1];
                        }
                        // PO,PO_Date,Ship_Company,Ship_Name,Ship_Addr,Ship_Addr_2,Ship_City,Ship_State,Ship_Zip,Ship_Country,Ship_Phone,Ship_Email,Ship_Service,CK_SKU,CK_Item,CK_Variant,Customized_Code,Customized_Msg,Customized_Code2,Customized_Msg2,Qty,Comment
                        //comment
                        values[7] = cus_comment + (string.IsNullOrEmpty(values[5]) ? "" : " " + values[5]);
                        values[7] = TrimTolength(values[7], 35);
                    }

                    oText += "," + string.Join(",", values);
                }
                else
                {
                    // CK_Item,CK_Variant,Customized_Code,Customized_Msg,Customized_Code2,Customized_Msg2,Qty,Comment";
                    // SAM: Need to take care of resource code here - comes as additional_field1
                    if (! String.IsNullOrEmpty(o.additional_field1))
                    {
                        oText +=
                            $",{o.Product.mfgid},{variant},{o.additional_field1},,{strMasterPakCode},{strMasterPakCodeMsg},{o.numitems},{cus_comment}";
                    }
                    else
                    {
                        oText +=
                            $",{o.Product.mfgid},{variant},{strMasterPakCode},{strMasterPakCodeMsg},,,{o.numitems},{cus_comment}";
                    }
                    
                }

                orderFinal.AppendLine(oText);
                o.Product = null;
                using (var context = new AutoCareDataContext(configData.ConnectionString))
                {
                    context.Entry(o).State = EntityState.Modified;
                    context.OrderItems.Attach(entity: o);
                    o.variant_id = variant;
                    context.Entry(o).Property(I => I.variant_id).IsModified = true;
                    context.SaveChanges();
                }
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
                if (!string.IsNullOrEmpty(mfgId))
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

            }
            if (!string.IsNullOrEmpty(variant) && variant.Substring(0, 1) == "-")
                variant = variant.Substring(1);
            return variant;
        }


        public static orders GetOrderFromPO(string connectionString, string poNumber)
        { 
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.Orders.Where(ord => ord.po_no == poNumber).FirstOrDefault();
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
                return context.Orders.Include(I => I.order_items).Where(whereFunc).ToList();
            }
        }

        public static void FixOrderDetails(string connectionString,
        string orderNo, string serialNo, string status, string shipAgent,
        string shipServiceCode, string trackingNo, string trackingLink, string mfgItemID, string variantID, int sequenceNo, int orderItemId)
        {

        }

        public static bool UpdateOrderDetail(string connectionString,
        string orderNo, string serialNo, string status, string shipAgent,
        string shipServiceCode, string trackingNo, string trackingLink, string mfgItemID, string variantID, int sequenceNo, 
        ref int totalItemsShipped, ref DateTime? lastShipDate)
        {
            bool orderStatusChanged = false;
            
            using (var context = new AutoCareDataContext(connectionString))
            {
                // Update order details according to CK status
                order_item_details order_det2 = context.OrderItemDetails.FirstOrDefault(I => I.order_no == orderNo && I.production_slno == serialNo);
                order_item_details order_det = null;

                if (order_det2 == null || order_det2.status != status)
                {
                    orderStatusChanged = true; 
                }
                if (order_det2 != null)
                {
                    if (orderStatusChanged)
                    {
                        Console.WriteLine(string.Format("   Existing Order Details record "));
                        order_det =  JsonConvert.DeserializeObject<order_item_details>(JsonConvert.SerializeObject(order_det2));
                        order_det.production_slno = serialNo;
                        order_det.status = status;
                        order_det.status_datetime = DateTime.Now;
                        order_det.ship_agent = shipAgent;
                        order_det.ship_service_code = shipServiceCode;
                        order_det.tracking_no = trackingNo;
                        order_det.tracking_link = trackingLink;
                        order_det.mfg_item_id = mfgItemID;
                        order_det.sku = mfgItemID.TrimEnd() + variantID.TrimEnd();
                        // SM: take care of Ship date
                        if (order_det.ship_date == null && status == "Shipped")
                            order_det.ship_date = order_det.status_datetime;

                        context.OrderItemDetails.AddOrUpdate(order_det);                      
                    }
                }
                else
                {
                    // Find ACG item ID from product table and mfgItemID
                    // Map ACG item ID and Variant ID with order_items table
                    // get order_item_id
                    //   insert in order_item_detail table && populate the order_item_detail table
                    // Update order details according to CK status
                    Console.WriteLine(string.Format("   New Order Details record "));
                    order_det = new order_item_details();
                    order_det.production_slno = serialNo;
                    order_det.status = status;
                    order_det.status_datetime = DateTime.Now;
                    order_det.ship_agent = shipAgent;
                    order_det.ship_service_code = shipServiceCode;
                    order_det.tracking_no = trackingNo;
                    order_det.tracking_link = trackingLink;
                    order_det.mfg_item_id = mfgItemID;
                    order_det.sku = mfgItemID.TrimEnd() + variantID.TrimEnd();
                    order_det.order_no = orderNo;
                    if (status == "Shipped")
                        order_det.ship_date = order_det.status_datetime;

                    order_det.sequence_no = sequenceNo;
                    // find order_item_id
                    var product_rec = context.Products.FirstOrDefault(I => I.mfgid == mfgItemID);
                    order_det.order_item_id = 0;
                    if (product_rec == null)
                    {
                        //SM: could not map order item id, needs manual check
                        // SM: need to handle error here: Order No: <orderno>: Status API does not match Products table. Mfg Item ID: <mfgItemID>. Serial no <serialNo>
                        Console.WriteLine(string.Format("   mfg ID from Status {0} not found in Products table. Status needs to be manually sent ", mfgItemID));
                    }
                    else
                    {
                        order_det.description = product_rec.description;
                        var orderItemList = context.OrderItems.Where(I => I.itemid == product_rec.SKU && I.variant_id == variantID).ToList();
                        if (orderItemList.Count == 0)
                        {
                            //SM: could not map order item id, needs manual check
                            // SM: need to handle error here: Order No: <orderno>: Status API does not match. Order_Items table- ACG ItemID: <product_rec.SKU>, Variant ID: variantID, Serial no <serialNo>     
                            Console.WriteLine(string.Format("   Item {0} and / or Varianr {1} not found in Order_Items table table. Status needs to be maniually sent ", product_rec.SKU, variantID));
                        }
                        else
                        {
                            foreach (var thisOrderItem in orderItemList)
                            {
                                // check how many of these order_item_id are present in details table
                                int nItemsInDet = context.OrderItemDetails.Where(i => i.order_item_id == thisOrderItem.order_item_id).Count();

                                // check the next order_items record if all the quantities are in detail table
                                if (nItemsInDet >= thisOrderItem.numitems)   // should never be > but to be on the safe side ...
                                    continue;
                                // we can use this order_item_id
                                order_det.order_item_id = thisOrderItem.order_item_id;
                                Console.WriteLine(string.Format("   Using order item id {0}", thisOrderItem.order_item_id));
                                break;
                            }
                        }
                    }
                    // SM: if no order_items match, it will be 0
                    context.OrderItemDetails.Add(order_det);                  
                }
                // SM - get values back to calling fn
                if (order_det != null && order_det.status == "Shipped")
                {
                    totalItemsShipped++;
                    if (order_det.ship_date > lastShipDate)
                        lastShipDate = order_det.ship_date;
                }
                context.SaveChanges();
                return orderStatusChanged;
            }
        }

        public static string GetCustomerPOFromOrderNo(string connectionString, string thisorderNo)
        {
            string customer_PO = "";
            using (var context = new AutoCareDataContext(connectionString))
            {
                orders thisOrder = context.Orders.FirstOrDefault(I => I.orderno == thisorderNo);
                if (thisOrder != null)
                {
                    if (!string.IsNullOrEmpty(thisOrder.po_no))
                        customer_PO = thisOrder.po_no.Trim();
                }
            }
            return customer_PO;
        }

        public static List<orders> GetOrderWithDetails(string connectionString, List<string> orderNos)
        {
            List<orders> li = new List<orders>();
            using (var context = new AutoCareDataContext(connectionString))
            {
                //foreach (var order in orderNos)
                //{
                    var orders = (from o in context.Orders.Include(I => I.order_shipments).Include(I => I.order_items)
                                   where orderNos.Contains(o.orderno)
                                   select o).ToList();
                if (orders.Any())
                {
                    foreach (var order in orders)
                    {
                        order.order_item_details = context.OrderItemDetails.Where(I => I.order_no == order.orderno)
                            .ToList();
                    }
                    li = orders.ToList();
                }
                //}
            }
            
            return li;
        }

        public static List<order_item_details> GetOrderItemDetail(string connectionString,
            Func<order_item_details, bool> condFunc)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.OrderItemDetails.Where(condFunc).ToList();
            }
        }
    }
}
