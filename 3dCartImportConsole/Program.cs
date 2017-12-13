using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations;
using AutoCarOperations.DAL;
using AutoCarOperations.Model;
using DCartRestAPIClient;
using Newtonsoft.Json;
using _3dCartImportConsole.CKOrderStatus;
using _3dCartImportConsole.Helper;
using Order = DCartRestAPIClient.Order;

namespace _3dCartImportConsole
{
    class Program
    {
        private static int? acg_invoicenum;
        public static int numDaysToSync = 2;
        static void Main(string[] args)
        {
            var SegmentToProcess = string.Empty;  // 1: Import JFW orders, 2: PlaceOrder, 3: UpdateOrderStatus from CK API, ALL: All
            if (args.Length > 0)
            {
                SegmentToProcess = args[0];
                if (SegmentToProcess.StartsWith("/"))
                    SegmentToProcess = SegmentToProcess.Replace("/", "");
                if (SegmentToProcess.StartsWith("-"))
                    SegmentToProcess = SegmentToProcess.Replace("/", "");

                if (args.Length > 1 && SegmentToProcess == "2")
                    numDaysToSync = Convert.ToInt16(args[1]);
            }

            if (String.IsNullOrEmpty(SegmentToProcess ) )
            {
                Log.Info("Usage: ");
                Log.Info(" ACGOrderProcessing 1  := Process JFW Orders ");
                Log.Info(" ACGOrderProcessing 2 [<numDays>] := Import 3D Cart orders and create CK Orders. <numDays>: Number of days to go back for order sync");
                Log.Info(" ACGOrderProcessing 3: Update Order status from CK and create/upload JFW Tracking info ");
                return;
            }
            
            ConfigurationData configData = GetConfigurationDetails();
            string filePath =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string coverKingTrackingPath = Path.Combine(filePath, "./CoverKingTrackingFiles/");
            string incomingOrdersFilePath = Path.Combine(filePath, "./JFW/Orders/");
            string JFWTrackingFilePath = Path.Combine(filePath, "./JFW");
            string errorFilePath = Path.Combine(filePath, "./JFW/OrderErrors/");
            string processedFilePath = Path.Combine(filePath, "./ProcessedOrders/");
            string variantFilePath = Path.Combine(filePath, "./VariantFiles/");

            // *** UNcomment this section for processing variant files from ftp
            #region ProcessVarintFileFromFtp
            //Prepare Variant List from local path
            /***
            string path = variantFilePath + "Input/";
            FTPHandler.DownloadOrUploadOrDeleteFile(configData._3dCartFTPAddress, configData._3dCartFTPUserName, configData._3dCartFTPPassword, path, "", WebRequestMethods.Ftp.ListDirectory);
            DirectoryInfo variantDir = new DirectoryInfo(path);
            foreach (var file in variantDir.GetFiles("*.csv"))
            {
                var variantListCollection = GetDataTableFromCsv(file.FullName, true);
                CKVariantDAL.DeleteCKVariant(configData.ConnectionString);
                foreach (var variantList in variantListCollection)
                {
                    CKVariantDAL.SaveCKVariant(configData.ConnectionString, variantList);
                }
                FTPHandler.DownloadOrUploadOrDeleteFile(configData._3dCartFTPAddress, configData._3dCartFTPUserName, configData._3dCartFTPPassword, path, file.Name, WebRequestMethods.Ftp.DeleteFile);
                var filetomove = variantFilePath + "Release/" + file.Name;
                if (! File.Exists(filetomove))
                    File.Move(file.FullName, variantFilePath + "Release/" + file.Name);

                // SM: TODO: Sync main ck_variant table with temp ck variant table
            }
            ***/
            #endregion

            #region ProcessJFWOrderFile
            if (SegmentToProcess.ToUpper() == "ALL" || SegmentToProcess =="1")
            {

                //First Sync orders but DO NOT create orders or upload order files
                Log.Info("\r\nProcessing JFW Orders. First sync 3DCart orders to get the latest JFW order.");
                OrderDAL.PlaceOrder(configData, false, true, true,null ,20);
                //Download order from JFW FTP and place order
                var customer = CustomerDAL.FindCustomer(configData, customers => customers.billing_firstname == "JFW");
                acg_invoicenum = OrderDAL.GetMaxInvoiceNum(configData.ConnectionString, "ACGA-");
                Log.Info(string.Format("  New Invoice no for JFW starting with {0} ", acg_invoicenum+1));
                Log.Info("  Downloading JFW Orders from ftp ");
                FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, incomingOrdersFilePath, "", WebRequestMethods.Ftp.ListDirectory);
                DirectoryInfo dir = new DirectoryInfo(incomingOrdersFilePath);
                foreach (var file in dir.GetFiles("*.txt"))
                {
                    try
                    {
                        Log.Info(string.Format("\r\n  Processing Order File {0} ", file.Name));
                        string text = File.ReadAllText(file.FullName);
                        string[] lines = text.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
                        string error = string.Empty;
                        var jfw_orders = Get3dCartOrder(configData.NewJFWOrderStatusID, configData.ConnectionString, lines, customer, ref error);

                        if (jfw_orders.Item2.Count > 0)   // indicates incoming file could be processed
                        {
                            // SM: Oct 16 ** Need to make sure this PO is not duplicate in jfw_orders
                            OrderTrackingDAL.SaveJFWOrders(configData.ConnectionString, jfw_orders.Item2, file.Name);

                            // Send this file via email to the whole group - just in case - may need to take out later.
                            Log.Info(string.Format("  Emailing Order file to Team: ", file.Name));
                            MandrillMail.SendEmail(configData.MandrilAPIKey,
                                        "New JFW PO", "See attached file.", "team@autocareguys.com",
                                        file.FullName, file.Name, file.Extension);

                            // SM: Oct 13: Should we proceed even if error occurs in previous step ?
                            foreach (var order in jfw_orders.Item1)
                            {
                                // Push order to 3DCart
                                Log.Info(string.Format("  Creating 3DCart Order for {0}-{1} ", order.InvoiceNumberPrefix, order.InvoiceNumber));
                                var recordInfo = RestHelper.AddRecord(order, "Orders", configData.PrivateKey,
                                    configData.Token, configData.Store);

                                if (recordInfo.Status == ActionStatus.Failed)
                                {
                                    // Email error to "support@autocareguys.com"
                                    Log.Info(string.Format("    ** 3DCart Order creation failed "));
                                    MandrillMail.SendEmail(configData.MandrilAPIKey,
                                        "Failed to enter record in 3dCart. Please see the attached recordset", JsonConvert.SerializeObject(order), "support@autocareguys.com",
                                        file.FullName, file.Name, file.Extension);
                                }
                                order.OrderID = Convert.ToInt16(recordInfo.ResultSet);
                            }
                        }
                        //Delete JFW order file from FTP
                        Log.Info(string.Format("    Deleting JFW PO file from ftp: {0} ", file.Name));
                        FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, processedFilePath, file.Name, WebRequestMethods.Ftp.DeleteFile);

                        var destFile = "";
                        if (!string.IsNullOrEmpty(error))
                        {
                            // Email order creation error to "support@autocareguys.com"
                            Log.Info(string.Format("    Sending error email to Support "));
                            MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Processing Failed: " + file.Name, error, "support@autocareguys.com"
                                , file.FullName, file.Name, file.Extension);
                            destFile = errorFilePath + file.Name;
                            if (!File.Exists(destFile))
                                File.Move(file.FullName, destFile);
                        }
                        else
                        {
                            destFile = processedFilePath + file.Name;
                            if (!File.Exists(destFile))
                                File.Move(file.FullName, destFile);
                        }
                    }
                    catch (Exception e)
                    {
                        // Email generic error to "support@autocareguys.com"
                        Log.Error(string.Format("    Unexpected message {0} ", e.Message));
                        MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Processing Failed", e.Message, "support@autocareguys.com");
                    }
                }
            }
            #endregion


            #region Create_UpdateOrderFrom3DCart
            if (SegmentToProcess.ToUpper() == "ALL" || SegmentToProcess == "2")
            {
                Log.Info("\r\n*** Fetching 3D Cart Order and Creating CK Orders");
                // Get ONLY new orders, create order file and upload
                
                OrderDAL.PlaceOrder(configData, false, true, true, null, numDaysToSync);
            }
            #endregion

            #region UpdateStatusUsingCKStatusAPI
            if (SegmentToProcess.ToUpper() == "ALL" || SegmentToProcess == "3")
            {
                Log.Info("\r\n*** Fetching CK Order Status, Update JFW tracking and send Shipping Emails");
                // var orders = OrderDAL.FetchOrders(configData.ConnectionString, ord => ord.shipcomplete == "Submitted" && ord.order_status == 1);
                var orders = OrderDAL.FetchOrders(configData.ConnectionString, ord => ord.orderdate >= DateTime.Parse("10/01/2017") && (ord.order_status ==1 || ord.order_status == 4) );
                CKOrderStatus.Order_StatusSoapClient client = new Order_StatusSoapClient();
                Orders_response Response = new Orders_response();
                List<Parts> partList = new List<Parts>();
                //todo:group 5 orders
                foreach (var o in orders)
                {
                    try
                    {
                        Log.Info(string.Format("  Getting CK status for Order {0}", o.orderno));
                        var ss = client.CustomSet1(o.orderno, configData.AuthUserName);

                        var s = client.Get_OrderStatus_by_PO(configData.CoverKingAPIKey, o.orderno, configData.AuthUserName);
                        Log.Info(string.Format("   ... Number of Line Items =  {0}", s.Orders_list.Length));
                        if (s.Orders_list != null && s.Orders_list.Length > 0)
                        {
                            var ordersWithStatus = s.Orders_list[0];
                            foreach (var partStatus in ordersWithStatus.Parts_list)
                            {
                                if (partStatus != null)
                                {
                                    Log.Info(string.Format("   Status for ItemNo {0}, Variant {1}, Sl No: {2} : {3} Tracking {4} ", 
                                        partStatus.ItemNo, partStatus.VariantID, partStatus.Serial_No, partStatus.Status, partStatus.Package_No));
                                    //SM: Ignore cancelled status from CK API. This may be because the order was temporarily cancelled.
                                    if (partStatus.Status.ToLower() == "cancelled")
                                        continue;

                                    var sequenceNo = 1;
                                    bool statusChanged = OrderDAL.UpdateOrderDetail(configData.ConnectionString, o.orderno, partStatus.Serial_No,
                                        partStatus.Status, partStatus.Shipping_agent_used,
                                        partStatus.Shipping_agent_service_used,
                                        partStatus.Package_No, partStatus.Package_link, partStatus.ItemNo, partStatus.VariantID, sequenceNo);
                                    sequenceNo += 1;
                                    if (statusChanged)
                                    {
                                        partList.Add(partStatus);
                                    }
                                    continue;   //****** Temporary to avoid SHipment problem
                                }
                            }

                        }
                        //send email only if shipped
                        //update shipping information on 3dcart
                    }
                    catch (Exception e)
                    {

                    }
                }
                //Send Email to users
                if (partList.Any())
                {
                    List<orders> orderwithdetails = OrderDAL.GetOrderWithDetails(configData.ConnectionString,
                        partList.Select(I => I.Customer_PO).ToList());
                    foreach (var order in orderwithdetails)
                    {
                        var orderList = new List<object>();
                        foreach (var item in order.order_item_details)
                        {
                            orderList.Add(new
                            {
                                TRACKINGLINK = item.tracking_link,
                                TRACKINGNO = item.tracking_no,
                                SKU = item.sku,
                                DESCRIPTION = item.description,
                                SHIPDATE = item.status_datetime?.ToString("dd-MMM-yyyy") ?? ""
                            });
                        }
                        if (order.shipemail != null)
                        {
                            MandrillMail.SendEmailWithTemplate(configData.MandrilAPIKey,
                                "Order Success", "", "israhulroy@gmail.com", new ordertemplate
                                {
                                    Name = $"{order.shipfirstname} {order.shiplastname}",
                                    OrderNo = order.orderno,
                                    Contact = order.shipphone,
                                    Address = $"{order.shipaddress} {order.shipaddress2}",
                                    City = order.shipcity,
                                    State = order.shipstate,
                                    ZipCode = order.shipzip,
                                    OrderList = orderList
                                });
                        }
                        if (order.billemail != null)
                        {
                            MandrillMail.SendEmailWithTemplate(configData.MandrilAPIKey,
                                "Order Success", "", "israhulroy@gmail.com", new ordertemplate
                                {
                                    Name = $"{order.billfirstname} {order.billlastname}",
                                    OrderNo = order.orderno,
                                    Contact = order.billphone,
                                    Address = $"{order.billaddress} {order.billaddress2}",
                                    City = order.billcity,
                                    State = order.billstate,
                                    ZipCode = order.billzip,
                                    OrderList = orderList
                                });
                        }
                    }
                }
                //Update Order Status
                List<orders> updateOrderList = new List<orders>();
                foreach (var order in orders)
                {   
                    bool allShipped = true;
                    if (!(null == order.order_item_details))
                    {
                        foreach (var detail in order.order_item_details)
                        {
                            if (detail.status != "Shipped")
                            {
                                allShipped = false;
                                break;
                            }

                        }
                    }
                    else
                        allShipped = false;
                    if (allShipped)
                    {
                        updateOrderList.Add(order);
                    }
                }
                if (updateOrderList.Count > 0)
                {
                    OrderDAL.UpdateStatus(configData.ConnectionString, updateOrderList, "Shipped", 4);

                    foreach (var o in updateOrderList)
                    {
                        var records = RestHelper.GetRestAPIRecords<Shipment>("", string.Format("Orders/{0}/Shipments", o.order_id), configData.PrivateKey, configData.Token, configData.Store, "100", 0);
                        List<Shipment> li = new List<Shipment>();
                        foreach (var ship in records)
                        {
                            ship.ShipmentID = 0;
                            ship.ShipmentState = "Shipped";
                            ship.ShipmentOrderStatus = 4;
                            li.Add(ship);
                        }
                        //Update Shipment Information
                        RestHelper.UpdateShipmentRecord(li, "Orders", configData.PrivateKey, configData.Token,
                            configData.Store, o.order_id);
                    }
                }

                // Process Tracking information - Not needed any more
                FTPHandler.DownloadOrUploadOrDeleteFile(configData.FTPAddress, configData.FTPUserName, configData.FTPPassword, coverKingTrackingPath, "Tracking", WebRequestMethods.Ftp.ListDirectory, 50);
                var trackingList = ReadTrackingFile(coverKingTrackingPath + "/Tracking");
                OrderTrackingDAL.SaveOrderTracking(configData.ConnectionString, trackingList);

                var jfwFilename = "JFW-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt";
                string strFilePath = string.Format("{0}\\JFW", filePath);

                string strFileNameWithPath = string.Format("{0}\\Tracking\\{1}", JFWTrackingFilePath, jfwFilename);
                Log.Info(string.Format("  Creating JFW Tracking File {0} ", strFileNameWithPath));
                //string strTextHeader = "JFW PO_No,Tracking_No";
                //File.WriteAllText(strFileNameWithPath, strTextHeader + "\r\n");
                //var jfwFilteredList = OrderTrackingDAL.GetOrderTracking(configData.ConnectionString);

                // SM: need to filter only JFW orders. Preferably by BillEmail address. Later these need to be in a separate table.
                var jfwFilteredList = partList.Where(I => I.Status.ToLower() == "shipped" && I.Customer_PO.ToUpper().StartsWith("ACGA-"));
                if (jfwFilteredList != null && jfwFilteredList.Any())
                {
                    var lastPo = jfwFilteredList.LastOrDefault().Customer_PO;
                    foreach (var jfwOrder in jfwFilteredList)
                    {
                        //SM Oct 21: We need the Original PO - not the PO coming from part status
                        //  Customer_PO in partstatus is actually the ACG order no
                        string JFW_PO = OrderDAL.GetCustomerPOFromOrderNo(configData.ConnectionString, jfwOrder.Customer_PO);
                        if (JFW_PO.Length > 0)
                        {
                            string text = string.Format("{0}, {1}", JFW_PO, jfwOrder.Package_No.Trim());
                            Log.Info(string.Format("  Include Tracking: JFW PO {0}, ACG Order {1}, Tracking no ", JFW_PO, jfwOrder.Customer_PO, jfwOrder.Package_No.Trim()));
                            if (jfwOrder.Customer_PO != lastPo)
                            {
                                text = text + Environment.NewLine;
                            }
                            File.AppendAllText(strFileNameWithPath, text);
                        }
                    }
                    Log.Info(string.Format("  Uploading Tracking file to ftp ", jfwFilename));
                    FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, JFWTrackingFilePath, "\\Tracking\\" + jfwFilename, WebRequestMethods.Ftp.UploadFile);
                    // FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, JFWTrackingFilePath, jfwFilename, WebRequestMethods.Ftp.UploadFile);
                    //OrderTrackingDAL.UpdateOrderStatus(configData.ConnectionString, trackingList);
                }
            }
            //File.Delete(strFilePath+ "\\Tracking\\" + jfwFilename);
            //DeleteAllFile(coverKingTrackingPath + "/Tracking");
            #endregion
        }

        static List<List<TempCKVariant>> GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            List<List<TempCKVariant>> ckVariantList = new List<List<TempCKVariant>>();
            string sql = @"SELECT * FROM [" + fileName + "]";
            using (OleDbConnection connection = new OleDbConnection(
                @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                ";Extended Properties=\"TEXT;HDR=" + header + "\""))
            {
                try
                {

                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(sql, connection))
                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        List<TempCKVariant> ckVariants = new List<TempCKVariant>();
                        //DataTable dataTable = new DataTable();
                        //dataTable.Locale = CultureInfo.CurrentCulture;
                        //adapter.Fill(dataTable);
                        //return dataTable;
                        long i = 0;
                        while (reader.Read())
                        {
                            ckVariants.Add(new TempCKVariant()
                            {
                                ItemID = reader["ItemID"]?.ToString(),
                                VariantId = reader["VariantID"]?.ToString(),
                                ProductFamily = reader["Product Family"]?.ToString(),
                                InventoryQty = reader["Inventory Qty"] != null ? Convert.ToInt32(reader["Inventory Qty"]) : (int?)null,
                                SKU = reader["SKU"]?.ToString(),
                                OEM = reader["OEM"]?.ToString(),
                                Blocked = reader["Blocked"]?.ToString(),
                                ProductCode = reader["Product Code"]?.ToString(),
                            });
                            i = i + 1;
                            if (i == 1000)
                            {
                                ckVariantList.Add(ckVariants);
                                i = 0;
                                ckVariants = new List<TempCKVariant>();
                            }
                        }
                        if (ckVariantList.Count > 0)
                        {
                            ckVariantList.Add(ckVariants);
                        }
                        return ckVariantList;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.StackTrace);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        public static Tuple<List<Order>, List<jfw_orders>> Get3dCartOrder(string NewJFWOrderStatusID, string connectionString, string[] lines, customers customer, ref string error)
        {
            if (acg_invoicenum == null)
            {
                acg_invoicenum = 200825;
            }
            List<Order> orderList = new List<Order>();
            List<jfw_orders> jfw_order_list = new List<jfw_orders>();
            Order order = new Order();
            order.InvoiceNumberPrefix = "ACGA-";
            order.OrderStatusID = Convert.ToInt32(NewJFWOrderStatusID);  //SM: READ from Config data, so we can control order creation from outside.
            order.Referer = "PHONE ORDER";
            order.SalesPerson = "RB";
            order.OrderDate = DateTime.Now;
            order.OrderID = 0;
            order.CardNumber = "-1";
            order.CardName = "JFW Items";
            MapCustomerDetailOrders(order: ref order, customer: customer);
            for (int i = 0; i < lines.Length; i++)
            {
                // Ignore header line, blank lines, and lines starting with "End"
                if (i > 0 && !String.IsNullOrWhiteSpace(lines[i]) && !(lines[i].ToLower().StartsWith("end")) )
                {
                    int noOfItems = 0;
                    string tempError = "";
                    acg_invoicenum = acg_invoicenum + 1;
                    order.InvoiceNumber = acg_invoicenum;//Convert.ToInt32(DateTime.Now.ToString("ddMM") + val);
                    var orderSer = JsonConvert.DeserializeObject<Order>(JsonConvert.SerializeObject(order));
                    orderSer.OrderItemList = new List<OrderItem>();
                    orderSer.ShipmentList = new List<Shipment>();
                    var jfw_order_map = GenerateOrder(connectionString, orderSer, lines[0], lines[i], ref noOfItems, ref tempError);
                    //SM: Note that jfw_order_map may return two empty objects if lines cannot be processed
                    if (noOfItems == 0)
                    {
                        error += Environment.NewLine + tempError;
                        break;   // exit out of processing more lines.
                    }
                        
                    
                    if (string.IsNullOrEmpty(tempError))
                    {
                        orderSer = jfw_order_map.Item1;
                        orderList.Add(orderSer);
                    }
                    else
                        error += Environment.NewLine + tempError;

                    jfw_order_list.Add(jfw_order_map.Item2);
                    
                }
            }
            return Tuple.Create(orderList, jfw_order_list);
        }
        public static void MapCustomerDetailOrders(customers customer, ref Order order)
        {
            if (customer != null)
            {
                order.BillingAddress = customer.billing_address;
                order.BillingAddress2 = customer.billing_address2;
                order.BillingCity = customer.billing_city;
                order.BillingCountry = customer.billing_country;
                order.BillingEmail = customer.email;
                order.BillingFirstName = customer.billing_firstname;
                order.BillingLastName = customer.billing_lastname;
                order.BillingPhoneNumber = customer.billing_phone;
                order.BillingState = customer.billing_state;
                order.BillingZipCode = customer.billing_zip;
                order.CustomerID = customer.customer_id;
                order.BillingCompany = customer.billing_company;
                order.CustomerComments = customer.comments;
                order.BillingPaymentMethod = "Purchase Order";
                order.BillingOnLinePayment = false;
                order.BillingPaymentMethodID = "50";
                order.UserID = "storeadmin1";
            }
        }
        public static Tuple<Order, jfw_orders> GenerateOrder(string connectionString, Order order, string header, string text, ref int noOfItems, ref string error)
        {
            string[] splitHeader = header.Replace("\"", "").Split(',').Select(I => I.Trim()).ToArray();
            string[] splitText = text.Replace("\"", "").Split(',').Select(I => I.Trim()).ToArray();
            // SM: handle tab separated lines
            if (splitHeader.Length<2)
            {
                splitHeader = header.Replace("\"", "").Split('\t').Select(I => I.Trim()).ToArray();
                splitText = text.Replace("\"", "").Split('\t').Select(I => I.Trim()).ToArray();
            }
            int length = splitHeader.Length;
            var orderItem = new OrderItem();
            //orderItem. = new Product();
            var ship = new Shipment();
            var jfwOrder = new jfw_orders();
            string buyer = "500";
            noOfItems = 0;

            //SM: Oct 21: Need to check if header and lines have the same no of elements
            if (splitText.Length != length)
            {
                // cannot process, return empty order with error
                if (!string.IsNullOrEmpty(error))
                {
                    error = error + Environment.NewLine;
                }
                error += string.Format("Incoming PO File header and line do not have same number of items: \r\n  Header items:{0}, Line Items: {1}",
                    length, splitText.Length);
                Log.Info(string.Format("  ** Error in processing order {0} \r\n   ", error));
                return Tuple.Create(order, jfwOrder);
            }

            // Sep 7, Sam: take both formats - emailed or downloaded
            for (int i = 0; i< length; i++)
            {
                string variant = string.Empty;
                switch (splitHeader[i].ToUpper())
                {
                    case "PO": case "PO_NUMBER":
                        jfwOrder.PO = splitText[i];
                        order.PONo = jfwOrder.PO;
                        break;
                    case "PO_DATE":
                        jfwOrder.PO_Date = Convert.ToDateTime(splitText[i]);
                        break;
                    case "CK_SKU":  case "SKU":
                        jfwOrder.CK_SKU = splitText[i];
                        order.SKU = jfwOrder.CK_SKU;
                        if ((order.SKU.IndexOf("cdc", StringComparison.OrdinalIgnoreCase) >= 0) || (order.SKU.IndexOf("crd", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            ship.ShipmentCost = 5;
                        }
						/** SM: Check if this is needed any more 
                        //var ckVariant = ProductDAL.FindOrderFromSKU(connectionString, order.SKU);
                        //if (ckVariant != null)
                        //{
                        //    orderItem.ItemID = ckVariant.SKU;
                        //    //order.SKU = ckVariant.SKU;
                        //    orderItem.ItemOptionPrice = ckVariant.price * 70 / 100;
                        //    if (orderItem.ItemOptionPrice < 150)
                        //    {
                        //        ship.ShipmentCost = 5;
                        //    }
                        //    orderItem.CatalogID = ckVariant.catalogid;
                        //    orderItem.ItemDescription = ckVariant.description;
                        //}
                        //else
                        //{
                        //    throw new Exception("Product doesn't exists! " + JsonConvert.SerializeObject(order));
                        //}
						**/
                        break;
                    case "QTY":
                        jfwOrder.Qty = Convert.ToInt32(splitText[i]);
                        if (noOfItems == 0)
                        {
                            noOfItems = jfwOrder.Qty.Value;
                        }
                        orderItem.ItemQuantity = 1; break;
                    case "UNIT_COST":
                        orderItem.ItemUnitCost = Convert.ToDouble(splitText[i]);
                        break;
                    case "SHIP_NAME":
                        jfwOrder.Ship_Name = splitText[i];
                        string[] splitName = jfwOrder.Ship_Name.Split(' ');
                        ship.ShipmentFirstName = splitName[0];
                        ship.ShipmentOrderStatus = 11;
                        ship.ShipmentMethodName = "Custom Shipping";
                        ship.ShipmentNumber = 1;
                        ship.ShipmentLastName = jfwOrder.Ship_Name.Replace(ship.ShipmentFirstName, "").TrimStart();
                        if (ship.ShipmentLastName == "")
                        {
                            ship.ShipmentLastName = ship.ShipmentFirstName;
                            ship.ShipmentFirstName = "Mr./Ms.";
                        }
                        break;
                    case "SHIP_ADDR":
                    case "SHIP_ADDRESS_1":
                        jfwOrder.Ship_Addr = splitText[i];
                        ship.ShipmentAddress = jfwOrder.Ship_Addr;
                        break;
                    case "SHIP_ADDR_2":
                    case "SHIP_ADDRESS_2":
                        jfwOrder.Ship_Addr_2 = splitText[i];
                        ship.ShipmentAddress2 = jfwOrder.Ship_Addr_2;
                        break;
                    case "SHIP_CITY":
                        jfwOrder.Ship_City = splitText[i];
                        ship.ShipmentCity = jfwOrder.Ship_City;
                        break;
                    case "SHIP_STATE":
                        jfwOrder.Ship_State = splitText[i];
                        ship.ShipmentState = jfwOrder.Ship_State;
                        break;
                    case "SHIP_COUNTRY":
                        jfwOrder.Ship_Country = splitText[i];
                        ship.ShipmentCountry = jfwOrder.Ship_Country;
                        break;
                    case "SHIP_ZIP":
                    case "SHIP_POSTAL_CODE":
                        jfwOrder.Ship_Zip = splitText[i];
                        ship.ShipmentZipCode = jfwOrder.Ship_Zip;
                        break;
                    case "SHIP_PHONE":
                        jfwOrder.Ship_Phone = splitText[i];
                        ship.ShipmentPhone = jfwOrder.Ship_Phone.Trim();
                        if (string.IsNullOrEmpty(ship.ShipmentPhone))
                            ship.ShipmentPhone = "111-111-1111";
                        break;
                    case "SHIP_EMAIL":
                        jfwOrder.Ship_Email = splitText[i];
                        ship.ShipmentEmail = jfwOrder.Ship_Email;
                        break;
                    case "SHIP_COMPANY":
                        jfwOrder.Ship_Company = splitText[i];
                        ship.ShipmentCompany = jfwOrder.Ship_Company;
                        break;
                    case "SHIP_SERVICE":
                        jfwOrder.Ship_Service = splitText[i];
                        if (!string.IsNullOrEmpty(jfwOrder.Ship_Service))
                        {
                            order.InternalComments = Environment.NewLine;
                        }
                        order.InternalComments = "Ship_Service: " + jfwOrder.Ship_Service;
                        break;
                    case "CK_ITEM":
                        jfwOrder.CK_Item = splitText[i];
                        orderItem.ItemID = jfwOrder.CK_Item;
                        break;
                    case "CK_VARIANT":
                        jfwOrder.CK_Variant = splitText[i];
                        variant = splitText[i];
                        break;
                    case "CUSTOMIZED_CODE":
                        jfwOrder.Customized_Code = splitText[i];
                        if (!string.IsNullOrEmpty(order.InternalComments))
                        {
                            order.InternalComments = Environment.NewLine;
                        }
                        order.InternalComments = "Customized_Code: " + jfwOrder.Customized_Code;
                        break;
                    case "CUSTOMIZED_MSG":
                        jfwOrder.Customized_Msg = splitText[i];
                        if (!string.IsNullOrEmpty(order.InternalComments))
                        {
                            order.InternalComments = Environment.NewLine;
                        }
                        order.InternalComments = "Customized_Msg: " + jfwOrder.Customized_Msg;
                        break;
                    case "CUSTOMIZED_CODE2":
                        jfwOrder.Customized_Code2 = splitText[i];
                        if (!string.IsNullOrEmpty(order.InternalComments))
                        {
                            order.InternalComments = Environment.NewLine;
                        }
                        order.InternalComments = "Customized_Code2: " + jfwOrder.Customized_Code2;
                        break;
                    case "CUSTOMIZED_MSG2":
                        jfwOrder.Customized_Msg2 = splitText[i];
                        if (!string.IsNullOrEmpty(order.InternalComments))
                        {
                            order.InternalComments = Environment.NewLine;
                        }
                        order.InternalComments = "Customized_Msg2: " + jfwOrder.Customized_Msg2;
                        break;
                    case "COMMENT":
                        jfwOrder.Comment = splitText[i];
                        if (!string.IsNullOrEmpty(order.InternalComments))
                        {
                            order.InternalComments = Environment.NewLine;
                        }
                        order.InternalComments = "Comment: " + jfwOrder.Comment;
                        break;
                }
                if (i == (length - 1))
                {
      //              if (!string.IsNullOrEmpty(orderItem.ItemID) && orderItem.CatalogID != null)
      //              {
      //                  // SM: To check. Should not need catalogid -> it is a string
						//order.SKU = orderItem.ItemID + orderItem.CatalogID;
      //              }
                    if (string.IsNullOrEmpty(order.SKU))
                    {
                        if (!string.IsNullOrEmpty(error))
                        {
                            error = error + Environment.NewLine;
                        }
                        string thiserror = string.Format("Product does not exist in the system: \r\n  SKU:{0}, JFW PO No {1},  PO date: {2}, Ship to: {3} \r\n" ,
                            order.SKU, jfwOrder.PO, jfwOrder.PO_Date, jfwOrder.Ship_Name);
                        Log.Info(string.Format("  ** Error in processing order {0} \r\n   ", thiserror));
                        error += thiserror;
                    }
                    var ckVariant = ProductDAL.FindOrderFromSKU(connectionString, order.SKU);
                    if (ckVariant != null)
                    {
                        orderItem.ItemID = ckVariant.SKU;
                        //order.SKU = ckVariant.SKU;
                        orderItem.ItemOptionPrice = ckVariant.price * 70 / 100;
                        if (orderItem.ItemOptionPrice < 150)
                        {
                            ship.ShipmentCost = 5;
                        }
                        orderItem.CatalogID = ckVariant.catalogid;
                        orderItem.ItemDescription = ckVariant.description;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(error))
                        {
                            error = error + Environment.NewLine;
                        }
                        string thiserror = string.Format("SKU does not exist the system. \r\n  SKU:{0}, ItemNo: {4}, JFW PO No {1},  PO date: {2}, Ship to: {3} \r\n",
                            order.SKU, jfwOrder.PO, jfwOrder.PO_Date, jfwOrder.Ship_Name, orderItem.ItemID);
                        Log.Info(string.Format("  ** Error in processing order {0} \r\n   ", thiserror));
                        error += thiserror;

                    }
                    orderItem.ItemDescription = orderItem.ItemDescription +
                                                "<br><b>Vehicle Configuration</b>&nbsp;Ref:Coverking Part No: " +
                                                order.SKU;
                    order.CustomerComments = string.Format("PO NO: {0}; Buyer: {1}", order.PONo, buyer);
                    order.OrderItemList.Add(orderItem);
                    order.ShipmentList.Add(ship);
                    
                    if (noOfItems > 1)
                    {
                        noOfItems = noOfItems - 1;
                        GenerateOrder(connectionString, order, header, text, ref noOfItems, ref error);
                        Log.Info(string.Format("  JFW PO {2} Order No {0}-{1} ", order.InvoiceNumberPrefix, order.InvoiceNumber, order.PONo));
                    }
                }
            }
            return Tuple.Create(order, jfwOrder);
        }
        public static ConfigurationData GetConfigurationDetails()
        {
            return new ConfigurationData
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["mysqlconnection"].ConnectionString,
                PrivateKey = ConfigurationManager.AppSettings["PrivateKey"],
                Store = ConfigurationManager.AppSettings["Store"],
                Token = ConfigurationManager.AppSettings["Token"],
                AuthUserName = ConfigurationManager.AppSettings["AuthUserName"],
                AuthPassowrd = ConfigurationManager.AppSettings["AuthPassowrd"],
                FTPAddress = ConfigurationManager.AppSettings["FTPAddress"],
                FTPUserName = ConfigurationManager.AppSettings["FTPUserName"],
                FTPPassword = ConfigurationManager.AppSettings["FTPPassword"],
                MandrilAPIKey = ConfigurationManager.AppSettings["MandrilAPIKey"],
                JFWFTPAddress = ConfigurationManager.AppSettings["JFWFTPAddress"],
                JFWFTPUserName = ConfigurationManager.AppSettings["JFWFTPUserName"],
                JFWFTPPassword = ConfigurationManager.AppSettings["JFWFTPPassword"],
                CoverKingAPIKey = ConfigurationManager.AppSettings["CoverKingAPIKey"],
                _3dCartFTPAddress = ConfigurationManager.AppSettings["3dCartFTPAddress"],
                _3dCartFTPUserName = ConfigurationManager.AppSettings["3dCartFTPUserName"],
                _3dCartFTPPassword = ConfigurationManager.AppSettings["3dCartFTPPassword"],
                CKOrderFolder = ConfigurationManager.AppSettings["CKOrderFolder"],
                NewJFWOrderStatusID = ConfigurationManager.AppSettings["JFWNewOrderStatus"]
            };
        }

        public static List<order_tracking> ReadTrackingFile(string filePath)
        {
            DirectoryInfo dir = new DirectoryInfo(filePath);
            List<order_tracking> orderTrackingList = new List<order_tracking>();
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                string[] lines = System.IO.File.ReadAllLines(file.FullName);
                for (int i = 0; i < lines.Length; i++)
                {
                    order_tracking tracking = new order_tracking();
                    if (i >= 1)
                    {
                        string[] splitHeader = lines[0].Split(new []{'\t'}).Select(I => I.Trim()).ToArray();
                        if (splitHeader.Length <= 1)
                        {
                            splitHeader = lines[0].Split(',').Select(I => I.Replace("\"", "").Trim()).ToArray();
                        }
                        string[] splitText = lines[i].Split(new[] { '\t' }).Select(I => I.Trim()).ToArray();
                        // SM Sep 8: Need to arrest if a line has empty values - such as empty tracking no. In that case, do not take that line
                        // and send an email. Changed StringSplitOptions.RemoveEmptyEntries to None
                        if (splitText.Length <= 1)
                        {
                            splitText = lines[i].Split(new string[]{"\",\""}, StringSplitOptions.None).Select(I => I.Replace("\"", "").Trim()).ToArray();
                        }
                        for (int j =0; j<splitHeader.Length;j++)
                        {
                            switch (splitHeader[j])
                            {
                                case "PO No":
                                    tracking.po_no = splitText[j];
                                    break;
                                case "Order No":
                                    tracking.order_no = Convert.ToInt64(splitText[j]);
                                    break;
                                case "Order Date":
                                    tracking.order_date = Convert.ToDateTime(splitText[j]);
                                    break;
                                case "SKU":
                                    tracking.SKU = splitText[j];
                                    break;
                                case "Ship Address":
                                    tracking.ship_address= splitText[j];
                                    break;
                                case "ShipDate":
                                    tracking.ship_date = Convert.ToDateTime(splitText[j]);
                                    break;
                                case "Tracking No":
                                    tracking.tracking_no = splitText[j];
                                    break;
                                case "Ship Agent":
                                    tracking.ship_agent = splitText[j];
                                    break;
                                case "Ship Service":
                                    tracking.ship_service = splitText[j]; break;
                            }
                        }
                        orderTrackingList.Add(tracking);
                    }
                }
            }
            return orderTrackingList;
        }

        public static void DeleteAllFile(string filePath)
        {
            DirectoryInfo dir = new DirectoryInfo(filePath);
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                File.Delete(file.FullName);
            }
        }
    }
}
