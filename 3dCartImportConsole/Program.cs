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
using Order = DCartRestAPIClient.Order;

namespace _3dCartImportConsole
{
    class Program
    {
        private static int? acg_invoicenum;
        static void Main(string[] args)
        {
            var configData = GetConfigurationDetails();
            string filePath =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string coverKingTrackingPath = Path.Combine(filePath, "../../CoverKingTrackingFiles/");
            string incomingOrdersFilePath = Path.Combine(filePath, "../../JFW/Orders");
            string processedFilePath = Path.Combine(filePath, "../../ProcessedOrders/");

            //Prepare Variant List from local path
            string path = @"D:\RND\WizardTest\MyCar\Doc\ACG92_08222017-17-29-48_CDC\ACG92-20171009.csv";
            var variantList = GetDataTableFromCsv(path, true);
            foreach (var variant in variantList)
            {
                CKVariantDAL.SaveCKVariant(configData.ConnectionString, variant);
            }
            
            //Download order from JFW FTP and place order
            var customer = CustomerDAL.FindCustomer(configData, customers => customers.billing_firstname == "JFW");
            acg_invoicenum = OrderDAL.GetMaxInvoiceNum(configData.ConnectionString, "ACGTest-");
            FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, incomingOrdersFilePath, "", WebRequestMethods.Ftp.ListDirectory);
            DirectoryInfo dir = new DirectoryInfo(incomingOrdersFilePath);
            foreach (var file in dir.GetFiles("*.txt"))
            {
                try
                {
                    string text = File.ReadAllText(file.FullName);
                    string[] lines = text.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
                    string error = string.Empty;
                    var jfw_orders = Get3dCartOrder(configData.ConnectionString, lines, customer, ref error);
                    if (!string.IsNullOrEmpty(error))
                    {
                        MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Processing Failed", error, "cs@autocareguys.com");
                    }
                    OrderTrackingDAL.SaveJFWOrders(configData.ConnectionString, jfw_orders.Item2);
                    foreach (var order in jfw_orders.Item1)
                    {
                        // Push order to 3DCart
                        var recordInfo = RestHelper.AddRecord(order, "Orders", configData.PrivateKey,
                            configData.Token, configData.Store);

                        if (recordInfo.Status == ActionStatus.Failed)
                        {
                            MandrillMail.SendEmail(configData.MandrilAPIKey, 
                                "Failed to enter record in 3dCart. Please see the attached recordset", JsonConvert.SerializeObject(order), "support@autocareguys.com");
                        }
                        //send an email to support@autocareguys.com
                        order.OrderID = Convert.ToInt16(recordInfo.ResultSet);
                    }
                    //SM sep 8: First check if the file is present in ftp site before trying to delete.
                    FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, processedFilePath, file.Name, WebRequestMethods.Ftp.DeleteFile);
                    File.Move(file.FullName, processedFilePath + file.Name);
                }
                catch (Exception e)
                {
                    MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Processing Failed", e.Message, "support@autocareguys.com");
                }
            }

            OrderDAL.PlaceOrder(configData, false, true, false);
            var orders = OrderDAL.FetchOrders(configData.ConnectionString, ord => ord.shipcomplete == "Submitted");
            CKOrderStatus.Order_StatusSoapClient client = new Order_StatusSoapClient();
            Orders_response Response = new Orders_response();

            //group 5 orders
            foreach (var o in orders)
            {
                try
                {
                    var s = client.Get_OrderStatus_by_PO(configData.CoverKingAPIKey, o.orderno, configData.AuthUserName);
                    if (s.Orders_list != null && s.Orders_list.Length > 0)
                    {
                        var orderStatus = s.Orders_list[0];
                        var partStatus = orderStatus.Parts_list != null && orderStatus.Parts_list.Length > 0 ? orderStatus.Parts_list[0] : (Parts)null;
                        if (partStatus != null)
                        {
                            OrderDAL.UpdateOrderDetail(configData.ConnectionString, o.orderno, partStatus.Serial_No,
                                partStatus.Status, partStatus.Shipping_agent_used, partStatus.Shipping_agent_service_used,
                                partStatus.Package_No, partStatus.Package_link);

                            if (partStatus.Status.ToLower() == "shipped")
                            {
                                //MandrillMail.SendEmail(configData.MandrilAPIKey, "Order has been shipped", e.Message, "support@autocareguys.com");
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
            // Process Tracking information
            FTPHandler.DownloadOrUploadOrDeleteFile(configData.FTPAddress, configData.FTPUserName, configData.FTPPassword, coverKingTrackingPath, "Tracking", WebRequestMethods.Ftp.ListDirectory, 1);
            // string filePathWithName = Path.Combine(filePath, @"\BDL_ORDERS_20170818-1915-A.txt");
            var trackingList = ReadTrackingFile(coverKingTrackingPath + "/Tracking");
            OrderTrackingDAL.SaveOrderTracking(configData.ConnectionString, trackingList);
            var jfwFilename = "JFW-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt";
            string strFilePath = string.Format("{0}..\\..\\..\\JFW\\", filePath);
            string strFileNameWithPath = string.Format("{0}\\Tracking\\{1}", strFilePath, jfwFilename);
            //string strTextHeader = "JFW PO_No,Tracking_No";
            //File.WriteAllText(strFileNameWithPath, strTextHeader + "\r\n");
            var jfwFilteredList = OrderTrackingDAL.GetOrderTracking(configData.ConnectionString);
            if (jfwFilteredList != null && jfwFilteredList.Count > 0)
            {
                var lastPo = jfwFilteredList.LastOrDefault().Value.po_no;
                foreach (var jfwOrder in jfwFilteredList)
                {
                    string text = string.Format("{0}, {1}", jfwOrder.Key.Trim(), jfwOrder.Value.tracking_no.Trim());
                    if (jfwOrder.Value.po_no != lastPo)
                    {
                        text = text + Environment.NewLine;
                    }
                    File.AppendAllText(strFileNameWithPath, text);
                }
                FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, strFilePath, "\\Tracking\\" + jfwFilename, WebRequestMethods.Ftp.UploadFile);
                OrderTrackingDAL.UpdateOrderStatus(configData.ConnectionString, trackingList);
            }
            //File.Delete(strFilePath+ "\\Tracking\\" + jfwFilename);
            //DeleteAllFile(coverKingTrackingPath + "/Tracking");
            //acga > prefix
            //170801 > invoice
        }

        static List<List<CKVariant>> GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            List<List<CKVariant>> ckVariantList = new List<List<CKVariant>>();
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
                        List<CKVariant> ckVariants = new List<CKVariant>();
                        //DataTable dataTable = new DataTable();
                        //dataTable.Locale = CultureInfo.CurrentCulture;
                        //adapter.Fill(dataTable);
                        //return dataTable;
                        long i = 0;
                        while (reader.Read())
                        {
                            ckVariants.Add(new CKVariant()
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
                                ckVariants = new List<CKVariant>();
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
                    Console.WriteLine(e);
                    return null;
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        public static Tuple<List<Order>, List<jfw_orders>> Get3dCartOrder(string connectionString, string[] lines, customers customer, ref string error)
        {
            if (acg_invoicenum == null)
            {
                acg_invoicenum = 200825;
            }
            List<Order> orderList = new List<Order>();
            List<jfw_orders> jfw_order_list = new List<jfw_orders>();
            Order order = new Order();
            order.InvoiceNumberPrefix = "ACGTest-";
            order.OrderStatusID = 1;//Was 11 - Unpaid, now New - 1
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
                    
                    acg_invoicenum = acg_invoicenum + 1;
                    order.InvoiceNumber = acg_invoicenum;//Convert.ToInt32(DateTime.Now.ToString("ddMM") + val);
                    var orderSer = JsonConvert.DeserializeObject<Order>(JsonConvert.SerializeObject(order));
                    orderSer.OrderItemList = new List<OrderItem>();
                    orderSer.ShipmentList = new List<Shipment>();
                    var jfw_order_map = GenerateOrder(connectionString, orderSer, lines[0], lines[i], ref noOfItems, ref error);
                    orderSer = jfw_order_map.Item1;
                    jfw_order_list.Add(jfw_order_map.Item2);
                    orderList.Add(orderSer);
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
            int length = splitHeader.Length;
            var orderItem = new OrderItem();
            //orderItem. = new Product();
            var ship = new Shipment();
            var jfwOrder = new jfw_orders();
            string buyer = "500";
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
                    if (!string.IsNullOrEmpty(orderItem.ItemID) && orderItem.CatalogID != null)
                    {
                        order.SKU = orderItem.ItemID + orderItem.CatalogID;
                    }
                    if (string.IsNullOrEmpty(order.SKU))
                    {
                        if (!string.IsNullOrEmpty(error))
                        {
                            error = error + Environment.NewLine;
                        }
                        error = ("Product doesn't exists! SKU is empty. Please see the Json : " + JsonConvert.SerializeObject(order));
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
                        error = ("Product doesn't exists! variant is empty. Please see the Json : " + JsonConvert.SerializeObject(order));
                        //todo: throw error
                    }
                    orderItem.ItemDescription = orderItem.ItemDescription +
                                                "<br><b>Vehicle Configuration</b>&nbsp;Ref:Coverking Part No: " +
                                                order.SKU;
                    order.CustomerComments = string.Format("PO NO: {0}; Buyer: {1}", order.PONo, buyer);
                    order.OrderItemList.Add(orderItem);
                    order.ShipmentList.Add(ship);
                    //todo: Keep it commented for now
                    if (noOfItems > 1)
                    {
                        noOfItems = noOfItems - 1;
                        GenerateOrder(connectionString, order, header, text, ref noOfItems, ref error);
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
                CoverKingAPIKey = ConfigurationManager.AppSettings["CoverKingAPIKey"]
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
