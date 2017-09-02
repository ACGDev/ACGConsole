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

            /*string path = @"D:\RND\WizardTest\MyCar\Doc\ACG92_08222017-17-29-48_CDC\ACG92_08222017-17-29-48_CDC.csv";
            var variantList = GetDataTableFromCsv(path, true);
            foreach (var variant in variantList)
            {
                CKVariantDAL.SaveCKVariant(configData.ConnectionString, variant);
            }
            var customer = CustomerDAL.FindCustomer(configData, customers => customers.billing_firstname == "JFW");
            acg_invoicenum = OrderDAL.GetMaxInvoiceNum(configData.ConnectionString, "ACGA-");
            //remove the following line when we'll get actual FTP details
            FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, incomingOrdersFilePath, "", WebRequestMethods.Ftp.ListDirectory);
            DirectoryInfo dir = new DirectoryInfo(incomingOrdersFilePath);
            foreach (var file in dir.GetFiles("*.txt"))
            {
                try
                {
                    //List<string> content = new List<string>();
                    //FTPHandler.DownloadOrUploadFile(configData, filePath, "", ref content, WebRequestMethods.Ftp.ListDirectory);
                    string text = File.ReadAllText(file.FullName);
                    string[] lines = text.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
                    List<Order> orders = Get3dCartOrder(configData.ConnectionString, lines, customer);
                    foreach (var order in orders)
                    {
                        var recordInfo = RestHelper.AddRecord(order, "Orders", configData.PrivateKey,
                            configData.Token, configData.Store);
                        order.OrderID = Convert.ToInt16(recordInfo.ResultSet);
                    }
                    FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, processedFilePath, file.Name, WebRequestMethods.Ftp.DeleteFile);
                    File.Move(file.FullName, processedFilePath + file.Name);
                }
                catch (Exception e)
                {
                    MandrillMail.SendEmail(configData.MandrilAPIKey, "Order Processing Failed", e.Message, "cs@autocareguys.com");
                }
            }
            OrderDAL.PlaceOrder(configData, false, true, false);*/
            //FTPHandler.DownloadOrUploadOrDeleteFile(configData.FTPAddress, configData.FTPUserName, configData.FTPPassword, coverKingTrackingPath, "Tracking", WebRequestMethods.Ftp.ListDirectory, 2);
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
                    string text = string.Format("{0},{1}", jfwOrder.Key, jfwOrder.Value.tracking_no);
                    if (jfwOrder.Value.po_no != lastPo)
                    {
                        text = text + Environment.NewLine;
                    }
                    File.AppendAllText(strFileNameWithPath, text);
                }
            }
            FTPHandler.DownloadOrUploadOrDeleteFile(configData.JFWFTPAddress, configData.JFWFTPUserName, configData.JFWFTPPassword, strFilePath, "\\Tracking\\"+ jfwFilename, WebRequestMethods.Ftp.UploadFile);
            OrderTrackingDAL.UpdateOrderStatus(configData.ConnectionString, trackingList);
            File.Delete(strFilePath+ "\\Tracking\\" + jfwFilename);
            DeleteAllFile(coverKingTrackingPath + "/Tracking");
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
                                BodyType = reader["BodyType"].ToString(),
                                From_Year = reader["From_Year"] != null ? Convert.ToInt32(reader["From_Year"]) : (int?)null,
                                To_Year = reader["To_Year"] != null ? Convert.ToInt32(reader["To_Year"]) : (int?)null,
                                Jobber_Price = reader["Jobber_Price"] != null ? Convert.ToDouble(reader["Jobber_Price"]) : (double?)null,
                                Make_Descr = reader["Make_Descr"]?.ToString(),
                                Model_Descr = reader["Model_Descr"]?.ToString(),
                                Option = reader["Option"]?.ToString(),
                                Position = reader["Position"]?.ToString(),
                                ProductID = reader["ProductID"]?.ToString(),
                                SubModel = reader["SubModel"]?.ToString(),
                                VariantID = reader["VariantID"]?.ToString()
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
        public static List<Order> Get3dCartOrder(string connectionString, string[] lines, customers customer)
        {
            if (acg_invoicenum == null)
            {
                acg_invoicenum = 200825;
            }
            List<Order> orderList = new List<Order>();
            Order order = new Order();
            order.InvoiceNumberPrefix = "ACGA-";
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
                if (i > 0 && !String.IsNullOrWhiteSpace(lines[i]))
                {
                    int noOfItems = 0;
                    
                    acg_invoicenum = acg_invoicenum + 1;
                    order.InvoiceNumber = acg_invoicenum;//Convert.ToInt32(DateTime.Now.ToString("ddMM") + val);
                    var orderSer = JsonConvert.DeserializeObject<Order>(JsonConvert.SerializeObject(order));
                    orderSer.OrderItemList = new List<OrderItem>();
                    orderSer.ShipmentList = new List<Shipment>();
                    orderSer = GenerateOrder(connectionString, orderSer, lines[0], lines[i], ref noOfItems);
                    orderList.Add(orderSer);
                }
            }
            return orderList;
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
        public static Order GenerateOrder(string connectionString, Order order, string header, string text, ref int noOfItems)
        {
            string[] splitHeader = header.Replace("\"", "").Split(',').Select(I => I.Trim()).ToArray();
            string[] splitText = text.Replace("\"", "").Split(',').Select(I => I.Trim()).ToArray();
            int length = splitHeader.Length;
            var orderItem = new OrderItem();
            //orderItem. = new Product();
            var ship = new Shipment();

            for (int i = 0; i< length; i++)
            {
                switch (splitHeader[i])
                {
                    case "po_number":
                        order.PONo = splitText[i]; break;
                    case "sku":
                        order.SKU = splitText[i];
                        if ((order.SKU.IndexOf("cdc", StringComparison.OrdinalIgnoreCase) >= 0) || (order.SKU.IndexOf("crc", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            ship.ShipmentCost = 5;
                        }
                        var ckVariant = ProductDAL.FindOrderFromSKU(connectionString, order.SKU);
                        if (ckVariant != null)
                        {
                            orderItem.ItemID = ckVariant.SKU;
                            //order.SKU = ckVariant.SKU;
                            orderItem.ItemOptionPrice = ckVariant.price * 70 / 100;
                            if (orderItem.ItemOptionPrice < 60)
                            {
                                ship.ShipmentCost = 5;
                            }
                            orderItem.CatalogID = ckVariant.catalogid;
                            orderItem.ItemDescription = ckVariant.description;
                        }
                        else
                        {
                            throw new Exception("Product doesn't exists! " + JsonConvert.SerializeObject(order));
                        }
                        break;
                    case "qty":
                        if(noOfItems == 0)
                        {
                            noOfItems = Convert.ToInt32(splitText[i]);
                        }
                        orderItem.ItemQuantity = 1; break;
                    case "unit_cost":
                        orderItem.ItemUnitCost = Convert.ToDouble(splitText[i]);
                        //orderItem.ItemOptionPrice
                        break;
                    //case "core_price": break;
                    //case "total": break;
                    case "ship_name":
                        string[] splitName = splitText[i].Split(' ');
                        ship.ShipmentFirstName = splitName[0];
                        ship.ShipmentOrderStatus = 11;
                        ship.ShipmentMethodName = "Custom Shipping";
                        ship.ShipmentNumber = 1;
                        ship.ShipmentLastName = splitText[i].Replace(ship.ShipmentFirstName, "").TrimStart();
                        if (ship.ShipmentLastName == "")
                        {
                            ship.ShipmentLastName = ship.ShipmentFirstName;
                            ship.ShipmentFirstName = "Mr./Ms.";
                        }
                        break;
                    case "ship_address_1":
                        ship.ShipmentAddress = splitText[i]; break;
                    case "ship_address_2":
                        ship.ShipmentAddress2 = splitText[i]; break;
                    case "ship_city":
                        ship.ShipmentCity = splitText[i]; break;
                    case "ship_state":
                        ship.ShipmentState = splitText[i]; break;
                    case "ship_country":
                        ship.ShipmentCountry = splitText[i]; break;
                    case "ship_postal_code":
                        ship.ShipmentZipCode = splitText[i]; break;
                    case "ship_phone":
                        ship.ShipmentPhone = splitText[i].Trim();
                        if (string.IsNullOrEmpty(ship.ShipmentPhone))
                            ship.ShipmentPhone = "111-111-1111";
                        break;
                    case "buyer":
                        order.CustomerComments = string.Format("PO NO:{0}; Buyer: {1}", order.PONo, splitText[i].Trim());
                        break;
                }
                if (i == (length - 1))
                {
                    orderItem.ItemDescription = orderItem.ItemDescription +
                                                "<br><b>Vehicle Configuration</b>&nbsp;Ref:Coverking Part No: " +
                                                order.SKU;
                    order.OrderItemList.Add(orderItem);
                    order.ShipmentList.Add(ship);
                    if (noOfItems > 1)
                    {
                        noOfItems = noOfItems - 1;
                        GenerateOrder(connectionString, order, header, text, ref noOfItems);
                    }
                }
            }
            return order;
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
                        if (splitText.Length <= 1)
                        {
                            splitText = lines[i].Split(new string[]{"\",\""}, StringSplitOptions.RemoveEmptyEntries).Select(I => I.Replace("\"", "").Trim()).ToArray();
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
