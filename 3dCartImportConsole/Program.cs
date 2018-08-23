using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Forms;
using System.Windows.Forms.ComponentModel.Com2Interop;
using AmazonApp;
using AmazonApp.Helper;
using AmazonApp.Model;
using OrderItem = DCartRestAPIClient.OrderItem;

namespace _3dCartImportConsole
{
    class Program
    {
        private static int? acg_invoicenum;
        public static int numDaysToSync = 20;
        static void Main(string[] args)
        {
            var SegmentToProcess = "3";  // 1: Import JFW orders, 2: PlaceOrder, 3: UpdateOrderStatus from CK API, ALL: All
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

            if (String.IsNullOrEmpty(SegmentToProcess))
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
            double JFW_AR_2017 = 21893.03;  // updated Jan 26, 2018
            double JFW_AR_Ceiling = 25000.00;

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
            if (SegmentToProcess.ToUpper() == "ALL" || SegmentToProcess == "1")
            {
                // MessageBox.Show(string.Format("JFW balance in 2017: {0} - fix if needed", JFW_AR_2017));
                // Check JFW order total in 2018 so far
                //Double totalWip_Shipped = OrderDAL.FetchTotalOrderAmt(configData.ConnectionString, "2018-01-01", "support@justfeedwebsites.com");

                //First Sync orders but DO NOT create orders or upload order files
                Log.Info("\r\nProcessing JFW Orders. First sync 3DCart orders to get the latest JFW order.");
                OrderDAL.PlaceOrder(configData, false, true, true, null, 10);
                //Download order from JFW FTP and place order
                var customer = CustomerDAL.FindCustomer(configData.ConnectionString, customers => customers.billing_firstname == "JFW");
                acg_invoicenum = OrderDAL.GetMaxInvoiceNum(configData.ConnectionString, "ACGA-");
                //STOPPED  Log.Info(string.Format("  New Invoice no for JFW starting with {0} ", acg_invoicenum + 1));

                //STOPPED  Log.Info("  Downloading JFW Orders from ftp ");
                //STOPPED  FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, incomingOrdersFilePath, "", WebRequestMethods.Ftp.ListDirectory);

                DirectoryInfo dir = new DirectoryInfo(incomingOrdersFilePath);
                bool bProcessJFWOrder = false;
                /**
                if (JFW_AR_2017 + totalWip_Shipped >= JFW_AR_Ceiling && dir.GetFiles("*.txt").Length>0)
                {
                    
                    String emailMsg = String.Format(
                        "Sorry, {0} new orders cannot be processed as the credit limit has reached or exceeded for this account.\r\n" +
                        "Previous Year AR= {1} \r\n This Year WIP+Shipped = {2};\r\n Total {3} \r\n \r\n" +
                        "Please pay down the balance to resume processing orders.",
                        dir.GetFiles("*.txt").Length, JFW_AR_2017.ToString("C2"), totalWip_Shipped.ToString("C2"),
                        (JFW_AR_2017 + totalWip_Shipped).ToString("C2"));

                    Log.Info(string.Format("    Sending over limit email to JFW "));

                    MandrillMail.SendEmail(configData.MandrilAPIKey, "Cannot process new orders as Credit Limit has exceeded ", emailMsg,
                        "support@justfeedwebsites.com"); // support@justfeedwebsites.com
                    MandrillMail.SendEmail(configData.MandrilAPIKey, "Cannot process order as Credit Limit has exceeded ", emailMsg,
                        "sales@jfw6.com"); // support@justfeedwebsites.com
                    bProcessJFWOrder = false;
                }
                **/

                if (bProcessJFWOrder)
                {
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
                                foreach (Order order in jfw_orders.Item1)
                                {
                                    // Push order to 3DCart
                                    Log.Info(string.Format("  Creating 3DCart Order for {0}-{1} ", order.InvoiceNumberPrefix, order.InvoiceNumber));
                                    var recordInfo = RestHelper.AddRecord(order, "Orders", configData.PrivateKey,
                                        configData.Token, configData.Store);

                                    if (recordInfo.Status == ActionStatus.Failed)
                                    {
                                        // Email error to "support@autocareguys.com" => now to support@justfeedwebsites.com
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
                                // Email order creation error to "support@autocareguys.com" => now to support@justfeedwebsites.com
                                Log.Info(string.Format("    Sending error email to Support "));

                                MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Processing Failed: " + file.Name, error, "support@justfeedwebsites.com"
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
                var amazonOrders = GetAmazonOrders(configData);
                var manualAmazonOrderStatus = GetAmazonOrdersStatusFromExcel();

                Log.Info("\r\n*** Fetching CK Order Status, Update JFW tracking and send Shipping Emails");
                //var orders = OrderDAL.FetchOrders(configData.ConnectionString, ord => ord.order_status == 1 || ord.shipcomplete != "Shipped");
                // var orders = OrderDAL.FetchOrders(configData.ConnectionString, ord => ord.orderdate >= DateTime.Parse("10/01/2017") && (ord.order_status ==1 || ord.order_status == 4) );
                var orders = OrderDAL.FetchOrders(configData.ConnectionString, I => I.orderno == "ACGA-172095");
                Log.Info(String.Format("\r\n*** Number of Open Orders {0} ", orders.Count));

                // SM: Read Changed_ACGOrderNo.txt for substituted order numbers in CK
                String currentAppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                String changedOrderFileName = System.IO.Path.GetDirectoryName(currentAppPath) + "\\Changed_ACGOrderNo.txt";
                Log.Info(String.Format("\r\n*** Reading Changed Order file {0} ", changedOrderFileName));
                string textAll = File.ReadAllText(changedOrderFileName);
                string[] lines = textAll.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
                List<ChangedOrderNumbers> changedOrders = new List<ChangedOrderNumbers>();
                foreach (var thisLine in lines)
                {
                    if (thisLine.StartsWith("//") || thisLine.Trim().Length == 0)
                        continue;
                    var segments = thisLine.Split(',');
                    if (segments.Length == 2)
                    {
                        ChangedOrderNumbers c = new ChangedOrderNumbers();
                        c.ACG_OrderNo = segments[0].Trim();
                        c.Manuf_PO_No = segments[1].Trim();
                        changedOrders.Add(c);
                    }
                }

                Order_StatusSoapClient client = new Order_StatusSoapClient();
                Orders_response Response = new Orders_response();
                List<AmazonExcel> amazonPartList = new List<AmazonExcel>();
                List<Parts> partList = new List<Parts>();
                List<orders> shippedOrderList = new List<orders>();
                List<Parts> jfwShippedList = new List<Parts>();
                //todo:group 5 orders
                foreach (var o in orders)
                {
                    try
                    {
                        Log.Info(string.Format("  Getting CK status for Order {0}", o.orderno));
                        // var ss = client.CustomSet1(o.orderno, configData.AuthUserName);
                        //SM: First check if this order no was changed in CK
                        string thisOrderNoInCK = o.orderno;
                        if (changedOrders != null && changedOrders.Count > 0)
                        {
                            foreach (var chOrd in changedOrders)
                            {
                                if (chOrd.ACG_OrderNo == thisOrderNoInCK)
                                {
                                    thisOrderNoInCK = chOrd.Manuf_PO_No;
                                    break;
                                }
                            }
                        }

                        var s = client.Get_OrderStatus_by_PO(configData.CoverKingAPIKey, thisOrderNoInCK, configData.AuthUserName);
                        Log.Info(string.Format("   ... Number of Line Items =  {0}", s.Orders_list.Length));
                        if (s.Orders_list != null && s.Orders_list.Length > 0)
                        {
                            int totalItemsShipped = 0;
                            int sequenceNo = 0;
                            DateTime? lastShipDate = Convert.ToDateTime("01/01/2017");

                            foreach (var ordersWithStatus in s.Orders_list)
                            {
                                foreach (var partStatus in ordersWithStatus.Parts_list)
                                {
                                    if (partStatus == null)
                                        continue;

                                    //SM replace incoming PO no with ACG's order no, if order no was changed.
                                    if (partStatus.Customer_PO != o.orderno)
                                    {
                                        partStatus.Customer_PO = o.orderno;
                                    }
                                    Log.Info(string.Format("   Status for ItemNo {0}, Variant {1}, Sl No: {2} : {3} Tracking {4} ",
                                    partStatus.ItemNo, partStatus.VariantID, partStatus.Serial_No, partStatus.Status, partStatus.Package_No));
                                    //SM: Ignore cancelled status from CK API. This may be because the order was temporarily cancelled.
                                    if (partStatus.Status.ToLower() != "cancelled")
                                    {
                                        var shippedResult = SetPartStatus(partStatus, manualAmazonOrderStatus, o.po_no);
                                        if (amazonOrders.Any())
                                        {
                                            var amazonOrder =
                                                amazonOrders.FirstOrDefault(I => I.AmazonOrderId == o.po_no);
                                            if (amazonOrder != null && partStatus.Status.ToLower() == "shipped")
                                            {
                                                if (shippedResult == null)
                                                {
                                                    shippedResult = new AmazonExcel
                                                    {
                                                        StatusDate = DateTime.Now,
                                                        AmazonOrder = o.po_no,
                                                        ShipAgent = partStatus.Shipping_agent_used,
                                                        ShipService = partStatus.Shipping_agent_service_used,
                                                        TrackingNo = partStatus.Package_No
                                                    };
                                                    shippedResult.AmazonOrderItems = new List<KeyValuePair<string, decimal>>();
                                                    foreach (var res in amazonOrder.OrderItem)
                                                    {
                                                        shippedResult.AmazonOrderItems.Add(new KeyValuePair<string, decimal>(res.OrderItemId, res.QuantityOrdered));
                                                    }
                                                }
                                                amazonPartList.Add(shippedResult);
                                            }
                                        }

                                        sequenceNo += 1;
                                        bool statusChanged = OrderDAL.UpdateOrderDetail(configData.ConnectionString, o.orderno, partStatus.Serial_No,
                                            partStatus.Status, partStatus.Shipping_agent_used,
                                            partStatus.Shipping_agent_service_used,
                                            partStatus.Package_No, partStatus.Package_link, partStatus.ItemNo, partStatus.VariantID, sequenceNo,
                                            ref totalItemsShipped, ref lastShipDate);

                                        if (statusChanged)
                                        {
                                            partList.Add(partStatus);
                                        }
                                        if (partStatus.Status == "Shipped" && o.billemail == "support@justfeedwebsites.com")
                                            jfwShippedList.Add(partStatus);
                                    }
                                }
                                //*** SM: Why can't we take care of updating order status here if shipped ?
                                if (sequenceNo == totalItemsShipped && totalItemsShipped > 0)
                                {
                                    o.last_update = lastShipDate;
                                    shippedOrderList.Add(o);
                                }

                            }
                        } ////

                    }
                    catch (Exception e)
                    {

                    }
                }
                // SM: (new) Update Order Status - Also update order status in 3DCart
                if (shippedOrderList.Count > 0)
                {
                    OrderDAL.UpdateStatus(configData.ConnectionString, shippedOrderList, "Shipped", 4);

                    foreach (var o in shippedOrderList)
                    {
                        var records = RestHelper.GetRestAPIRecords<Shipment>("", string.Format("Orders/{0}/Shipments", o.order_id), configData.PrivateKey, configData.Token, configData.Store, "100", 0);
                        List<Shipment> li = new List<Shipment>();
                        foreach (var ship in records)
                        {
                            if (ship.ShipmentOrderStatus == 4)   // Already marked as shipped in 3DCart
                                continue;

                            ship.ShipmentID = 0;
                            //SM: This is STATE not Status ** ship.ShipmentState = "Shipped";
                            ship.ShipmentOrderStatus = 4;
                            if (o.last_update != null)
                                ship.ShipmentShippedDate = Convert.ToDateTime(o.last_update.ToString()).ToShortDateString();

                            // Try with this: ship.ShipmentTrackingCode... also  ship.ShipmentNumber
                            ship.ShipmentPhone = o.shipphone;
                            li.Add(ship);
                        }

                        //Update Shipment Information
                        if (li.Count > 0)
                        {
                            var status = RestHelper.UpdateShipmentRecord(li, "Orders", configData.PrivateKey, configData.Token,
                            configData.Store, o.order_id);
                        }

                    }
                }
                // SM ** handle JFW stuff here 
                // Process Tracking information - Not needed any more
                //FTPHandler.DownloadOrUploadOrDeleteFile(configData.FTPAddress, configData.FTPUserName, configData.FTPPassword, coverKingTrackingPath, "Tracking", WebRequestMethods.Ftp.ListDirectory, 50);
                //var trackingList = ReadTrackingFile(coverKingTrackingPath + "/Tracking");
                //OrderTrackingDAL.SaveOrderTracking(configData.ConnectionString, trackingList);
                if (jfwShippedList != null && jfwShippedList.Any())
                {
                    var jfwFilename = "JFW-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt";
                    // string strFilePath = string.Format("{0}\\JFW", filePath);

                    string strFileNameWithPath = string.Format("{0}\\Tracking\\{1}", JFWTrackingFilePath, jfwFilename);
                    Log.Info(string.Format("  Creating JFW Tracking File {0} ", strFileNameWithPath));
                    //string strTextHeader = "JFW PO_No,Tracking_No";
                    //File.WriteAllText(strFileNameWithPath, strTextHeader + "\r\n");
                    //var jfwFilteredList = OrderTrackingDAL.GetOrderTracking(configData.ConnectionString);

                    // SM: need to filter only JFW orders. Preferably by BillEmail address. Later these need to be in a separate table.
                    // not needed var jfwFilteredList = partList.Where(I => I.Status.ToLower() == "shipped" && I.Customer_PO.ToUpper().StartsWith("ACGA-"));

                    var lastPo = jfwShippedList.LastOrDefault().Customer_PO;
                    foreach (var jfwOrder in jfwShippedList)
                    {
                        //SM Oct 21: We need the Original PO - not the PO coming from part status
                        //  Customer_PO in partstatus is actually the ACG order no
                        string JFW_PO = OrderDAL.GetCustomerPOFromOrderNo(configData.ConnectionString, jfwOrder.Customer_PO);
                        if (JFW_PO.Length > 0)
                        {
                            string text = string.Format("{0}, {1}", JFW_PO, jfwOrder.Package_No.Trim());
                            Log.Info(string.Format("  Include Tracking: JFW PO {0}, ACG Order {1}, Tracking no {2}", JFW_PO, jfwOrder.Customer_PO, jfwOrder.Package_No.Trim()));
                            if (jfwOrder.Customer_PO != lastPo)
                            {
                                text = text + Environment.NewLine;
                            }
                            File.AppendAllText(strFileNameWithPath, text);
                        }
                    }
                    Log.Info(string.Format("  Uploading Tracking file to ftp ", jfwFilename));
                    FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, JFWTrackingFilePath, "\\Tracking\\" + jfwFilename, WebRequestMethods.Ftp.UploadFile);

                }
                if (amazonPartList.Count > 0)
                {
                    SubmitAmazonFeed(configData, amazonPartList);
                }
                List<orders> orderwithdetails = null;
                //Send Email to users
                if (partList.Any())
                {
                    orderwithdetails = OrderDAL.GetOrderWithDetails(configData.ConnectionString,
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
                                SHIPDATE = item.status == "Shipped" ? item.status_datetime.Value.ToString("dd-MMM-yyyy") : ""
                            });
                        }
                        //if we know the status value then we need to check with that
                        if (order.shipcomplete.ToLower() != "shipped")
                        {
                            /** Sam ** This section is commented temporarily. Need to fix Mandrill template etc and send **/
                            /**
                            if (! String.IsNullOrEmpty(order.shipemail))
                            {
                                MandrillMail.SendEmailWithTemplate(configData.MandrilAPIKey,
                                    "Order Success", "", "mukherjees2010@gmail.com", new ordertemplate
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
                            if (!String.IsNullOrEmpty(order.billemail))
                            {
                                MandrillMail.SendEmailWithTemplate(configData.MandrilAPIKey,
                                    "Order Success", "", "mukherjees2010@gmail.com", new ordertemplate
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
                        **/

                        }
                    }
                }
                //Update Order Status
                /** SM: moved above **
                if (orderwithdetails != null)
                {
                    List<orders> updateOrderList = new List<orders>();
                    foreach (var order in orderwithdetails)
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
                            var status = RestHelper.UpdateShipmentRecord(li, "Orders", configData.PrivateKey, configData.Token,
                                configData.Store, o.order_id);
                        }
                    }
                }
                **/


                /*** SM: This section moved above 
                // Process Tracking information - Not needed any more
                //FTPHandler.DownloadOrUploadOrDeleteFile(configData.FTPAddress, configData.FTPUserName, configData.FTPPassword, coverKingTrackingPath, "Tracking", WebRequestMethods.Ftp.ListDirectory, 50);
                //var trackingList = ReadTrackingFile(coverKingTrackingPath + "/Tracking");
                //OrderTrackingDAL.SaveOrderTracking(configData.ConnectionString, trackingList);

                var jfwFilename = "JFW-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt";
                // string strFilePath = string.Format("{0}\\JFW", filePath);

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
                ***/
            }
            //File.Delete(strFilePath+ "\\Tracking\\" + jfwFilename);
            //DeleteAllFile(coverKingTrackingPath + "/Tracking");
            #endregion
        }
        #region AMAZON
        private static AmazonExcel SetPartStatus(Parts partStatus, List<AmazonExcel> manualAmazonOrderStatus, string poNo)
        {
            if (partStatus.Shipping_agent_used == "WC")
            {
                var result = manualAmazonOrderStatus.FirstOrDefault(
                    I => I.AmazonOrder == poNo);
                if (result != null)
                {
                    partStatus.Shipping_agent_used = result.ShipAgent;
                    partStatus.Shipping_agent_service_used =
                        result.ShipService;
                    partStatus.Package_No = result.TrackingNo;
                    partStatus.Status = "Shipped";
                    return result;
                }
            }
            return null;
        }

        private static void SubmitAmazonFeed(ConfigurationData config, List<AmazonExcel> amazonOrders)
        {
            List<Dictionary<string, object>> liDict = new List<Dictionary<string, object>>();
            foreach (var amazon in amazonOrders)
            {
                if (amazon.ShipService == "R02")
                {
                    amazon.ShipService = "Ground";
                    amazon.ShipAgent = "FedEx";
                }
                foreach (var item in amazon.AmazonOrderItems)
                {
                    liDict.Add(new Dictionary<string, object>()
                    {
                        {"AmazonOrderId", amazon.AmazonOrder },
                        {"ShippedDate", amazon.StatusDate ?? DateTime.Now },
                        {"CarrierCode", amazon.ShipAgent },
                        {"ShippingMethod", amazon.ShipService },
                        {"TrackingNo", amazon.TrackingNo },
                        {"AmazonOrderItemCode", item.Key },
                        {"Quantity", item.Value }
                    });
                }
            }
            var config2 = new MarketplaceWebServiceConfig();
            // Set configuration to use US marketplace
            config2.ServiceURL = config.ServiceURL;
            // Set the HTTP Header for user agent for the application.
            config2.SetUserAgentHeader(
                config.AppName,
                config.Version,
                "C#");

            var amazonClient = new MarketplaceWebServiceClient(config.AccessKey,
                config.SecretKey,
                config2);

            SubmitFeedRequest request = new SubmitFeedRequest
            {
                Merchant = config.SellerId,
                FeedContent = FeedRequestXML.GenerateOrderFulfillmentFeed(config.SellerId, liDict)
            };
            // Calculating the MD5 hash value exhausts the stream, and therefore we must either reset the
            // position, or create another stream for the calculation.
            request.ContentMD5 = MarketplaceWebServiceClient.CalculateContentMD5(request.FeedContent);
            request.FeedContent.Position = 0;

            request.FeedType = "_POST_ORDER_FULFILLMENT_DATA_";

            var subResp = FeedSample.InvokeSubmitFeed(amazonClient, request);
            request.FeedContent.Close();
        }
        class AmazonExcel
        {
            public DateTime OrderDate { get; set; }
            public string AmazonOrder { get; set; }
            public string ACGOrder { get; set; }
            public string CKOrderNo { get; set; }
            public string ShipAgent { get; set; }
            public string ShipService { get; set; }
            public string TrackingNo { get; set; }
            public string UpdateAmazon { get; set; }
            public DateTime? ShipBy { get; set; }
            public DateTime? StatusDate { get; set; }
            public List<KeyValuePair<string, decimal>> AmazonOrderItems { get; set; }
        }
        private static List<AmazonExcel> GetAmazonOrdersStatusFromExcel()
        {
            String currentAppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            String fileName = "AmazonOpenOrders.csv";
            Log.Info(String.Format("\r\n*** Reading Amazon Manual Order status file {0} ", fileName));
            string sql = @"SELECT * FROM [" + fileName + "]";
            List<AmazonExcel> li = new List<AmazonExcel>();
            using (OleDbConnection connection = new OleDbConnection(
                @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + currentAppPath +
                ";Extended Properties=\"TEXT;HDR=Yes\""))
            {
                connection.Open();
                using (OleDbCommand command = new OleDbCommand(sql, connection))
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        li.Add(new AmazonExcel
                        {
                            OrderDate = Convert.ToDateTime(reader[0]),
                            ACGOrder = Convert.ToString(reader[5]),
                            AmazonOrder = Convert.ToString(reader[1]),
                            ShipBy = reader[2] != DBNull.Value ? Convert.ToDateTime(reader[2]) : (DateTime?)null,
                            CKOrderNo = Convert.ToString(reader[6]),
                            ShipAgent = Convert.ToString(reader[7]),
                            ShipService = Convert.ToString(reader[8]),
                            TrackingNo = Convert.ToString(reader[9]).TrimStart('\''),
                            StatusDate = reader[10] != DBNull.Value ? Convert.ToDateTime(reader["Status Date"]) : (DateTime?)null,
                            UpdateAmazon = Convert.ToString(reader[11])
                        });
                    }
                }
            }
            return li;
        }

        private static List<AmazonApp.Model.Order> GetAmazonOrders(ConfigurationData configData)
        {
            MarketplaceWebServiceOrdersConfig config = new MarketplaceWebServiceOrdersConfig();
            config.ServiceURL = configData.ServiceURL;
            // Set other client connection configurations here if needed
            // Create the client itself

            MarketplaceWebServiceOrders amazonClient = new MarketplaceWebServiceOrdersClient(configData.AccessKey,
                configData.SecretKey, configData.AppName, configData.Version, config);
            MarketplaceWebServiceOrdersSample amazonOrders = new MarketplaceWebServiceOrdersSample(amazonClient);
            var amazonResponse = amazonOrders.InvokeListOrders(true);
            foreach (var order in amazonResponse.ListOrdersResult.Orders)
            {
                var orderItemResponse = amazonOrders.InvokeListOrderItems(order.AmazonOrderId);
                order.OrderItem = orderItemResponse.ListOrderItemsResult.OrderItems;
            }
            return amazonResponse.ListOrdersResult.Orders;
        }
        #endregion

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
            order.SalesPerson = "";
            order.OrderDate = DateTime.Now;
            order.OrderID = 0;
            order.CardNumber = "-1";
            order.CardName = "JFW Items";
            MapCustomerDetailOrders(order: ref order, customer: customer);
            for (int i = 0; i < lines.Length; i++)
            {
                // Ignore header line, blank lines, and lines starting with "End"
                if (i > 0 && !String.IsNullOrWhiteSpace(lines[i]) && !(lines[i].ToLower().StartsWith("end")))
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
            if (splitHeader.Length < 2)
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
            for (int i = 0; i < length; i++)
            {
                string variant = string.Empty;
                switch (splitHeader[i].ToUpper())
                {
                    case "PO":
                    case "PO_NUMBER":
                        jfwOrder.PO = splitText[i];
                        order.PONo = jfwOrder.PO;
                        break;
                    case "PO_DATE":
                        jfwOrder.PO_Date = Convert.ToDateTime(splitText[i]);
                        break;
                    case "CK_SKU":
                    case "SKU":
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
                        if (!String.IsNullOrEmpty(jfwOrder.Customized_Msg))
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
                        string thiserror = string.Format("Product does not exist in the system: \r\n  SKU:{0}, JFW PO No {1},  PO date: {2}, Ship to: {3} \r\n",
                            order.SKU, jfwOrder.PO, jfwOrder.PO_Date, jfwOrder.Ship_Name);
                        Log.Info(string.Format("  ** Error in processing order {0} \r\n   ", thiserror));
                        error += thiserror;
                    }
                    var productAndDealerItems = ProductDAL.FindOrderFromSKU(connectionString, order.SKU);
                    if (productAndDealerItems != null && productAndDealerItems.Item1 != null && productAndDealerItems.Item2 != null)  // Sam  01/05/18
                    {
                        var product = productAndDealerItems.Item1;
                        var dealerPrice = productAndDealerItems.Item2;
                        orderItem.ItemID = product.SKU;  //  Sam 01/05/18
                        //order.SKU = ckVariant.SKU;
                        orderItem.ItemOptionPrice = Math.Round(dealerPrice.CostToDealer, 2);
                        ship.ShipmentCost = dealerPrice.ShipCost;

                        orderItem.CatalogID = product.catalogid;
                        orderItem.ItemDescription = product.description;
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

                    // SM: fix problem when qty > 2
                    for (var iCount = 2; iCount <= noOfItems; iCount++)
                    {
                        order.OrderItemList.Add(orderItem);
                        // order.ShipmentList.Add(ship);
                    }

                    /** Not needed
                    if (noOfItems > 1)
                    {
                        noOfItems = noOfItems - 1;
                        GenerateOrder(connectionString, order, header, text, ref noOfItems, ref error);
                        Log.Info(string.Format("  JFW PO {2} Order No {0}-{1} ", order.InvoiceNumberPrefix, order.InvoiceNumber, order.PONo));
                    }
                     **/
                }
            }
            return Tuple.Create(order, jfwOrder);
        }
        public static ConfigurationData GetConfigurationDetails()
        {
            return new ConfigurationData
            {
                AccessKey = ConfigurationManager.AppSettings["AccessKey"],
                SecretKey = ConfigurationManager.AppSettings["SecretKey"],
                AppName = ConfigurationManager.AppSettings["AppName"],
                Version = ConfigurationManager.AppSettings["Version"],
                ServiceURL = ConfigurationManager.AppSettings["ServiceURL"],
                SellerId = ConfigurationManager.AppSettings["SellerId"],
                MWSToken = ConfigurationManager.AppSettings["MWSToken"],
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
                        string[] splitHeader = lines[0].Split(new[] { '\t' }).Select(I => I.Trim()).ToArray();
                        if (splitHeader.Length <= 1)
                        {
                            splitHeader = lines[0].Split(',').Select(I => I.Replace("\"", "").Trim()).ToArray();
                        }
                        string[] splitText = lines[i].Split(new[] { '\t' }).Select(I => I.Trim()).ToArray();
                        // SM Sep 8: Need to arrest if a line has empty values - such as empty tracking no. In that case, do not take that line
                        // and send an email. Changed StringSplitOptions.RemoveEmptyEntries to None
                        if (splitText.Length <= 1)
                        {
                            splitText = lines[i].Split(new string[] { "\",\"" }, StringSplitOptions.None).Select(I => I.Replace("\"", "").Trim()).ToArray();
                        }
                        for (int j = 0; j < splitHeader.Length; j++)
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
                                    tracking.ship_address = splitText[j];
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
