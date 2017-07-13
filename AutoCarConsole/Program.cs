using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using AutoCarConsole.ACG_CK;
using AutoCarConsole.DAL;
using AutoCarConsole.Model;
using DCartRestAPIClient;
using MySql;
using MySql.Data.MySqlClient;
using Order = AutoCarConsole.ACG_CK.Order;

namespace AutoCarConsole
{
    class Program
    {
        public static List<order_items> orderItems = new List<order_items>();

        static void Main(string[] args)
        {
            ConfigurationData config = GetConfigurationDetails();
            Console.WriteLine("..........Fetch Customer Record..........");
            //List<Customer> customers = new List<Customer>();
            //var skip = 0;
            ///*  */
            //while (true)
            //{
            //    var customerGroups = RestHelper.GetRestAPIRecords<CustomerGroup>("", "CustomerGroups", config.PrivateKey, config.Token, config.Store, "100", 0, "");

            //    var records = RestHelper.GetRestAPIRecords<Customer>("", "Customers", config.PrivateKey, config.Token, config.Store, "200", skip, "");
            //    int counter = records.Count;
            //    Console.WriteLine("..........Fetches " + counter + " Customer Record..........");
            //    customers.AddRange(records);
            //    CustomerDAL custDAL = new CustomerDAL();
            //    custDAL.AddCustomers(customers, customerGroups);
            //    if (counter < 200)
            //    {
            //        break;
            //    }
            //    skip = 201 + skip;

            //    // customers.AddRange(records);
            //}

            //skip = 0;
            //string strOrderStart = DateTime.Today.AddMonths(-16).ToString("MM/dd/yyyy");
            //// Get first record that is not shipped or cancelled in the last 6 months
            //// To fetch ALL orders comment out the following portion
            //MySql.Data.MySqlClient.MySqlConnection conn;
            //string myConnectionString = ConfigurationManager.ConnectionStrings["mysqlconnection"].ToString();
            //try
            //{
            //    using (conn = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString))
            //    {

            //        MySqlCommand cmd = conn.CreateCommand();
            //        cmd.CommandText = "select orderdate from orders where Shipcomplete = 'Pending' order by orderdate limit 1";
            //        //Command to get query needed value from DataBase
            //        conn.Open();
            //        MySqlDataReader reader = cmd.ExecuteReader();

            //        if (reader.Read())
            //        {
            //            var result = reader.GetDateTime("orderdate");
            //            strOrderStart = result.ToString("MM/dd/yyyy");

            //        }
            //        conn.Close();
            //    }
            //}
            //catch (MySql.Data.MySqlClient.MySqlException ex)
            //{
            //    Console.WriteLine("Error " + ex.Message);
            //    return;
            //}

            //Console.WriteLine("..........Fetch Orders..........");
            //List<DCartRestAPIClient.Order> orders = new List<DCartRestAPIClient.Order>();



            //while (true)
            //{
            //    var records = RestHelper.GetRestAPIRecords<DCartRestAPIClient.Order>("", "Orders", config.PrivateKey, config.Token, config.Store, "100", skip, strOrderStart);
            //    int counter = records.Count;
            //    Console.WriteLine("..........Fetches " + counter + " Order Record..........");
            //    orders.AddRange(records);
            //    if (counter < 100)
            //    {
            //        break;
            //    }

            //    skip = 101 + skip;


            //}
            //OrderDAL odDAL = new OrderDAL();
            //var ordersDb = odDAL.AddOrders(orders);

            List<orders> ordersDB;
            using (var context = new AutoCareDataContext())
            {
                ordersDB = context.Orders.Include(I => I.order_items).Include("order_items.Product").Where(I => I.shipcomplete != "shipped" && I.shipcomplete != "Cancelled").ToList();
            }
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            foreach (var order in ordersDB)
            {

                //var ckOrders = GetCKOrder(order);
                //var json = Newtonsoft.Json.JsonConvert.SerializeObject(ckOrders);
                //if (ckOrders.Count > 0)
                //{
                //    var oro = ckOrders.ToArray();
                //    Order_PlacementSoapClient placeOrder = new Order_PlacementSoapClient("Order_PlacementSoap");
                //    try
                //    {
                //        placeOrder.Open();
                //        var response = placeOrder.Place_Orders(new Auth_Header()
                //        {
                //            DealerID = "SRG-TEST",
                //            Password = "SRG45675"
                //        }, oro);
                //    }
                //    catch (FaultException e)
                //    {
                //        placeOrder.Close();
                //        throw;
                //    }
                //}
                RestHelper.Execute(@"http://api.coverking.com/orders/Order_Placement.asmx?op=Place_Orders", "SRG-TEST", "SRG45675", order);

            }
        }

        public static string GetVariant(string mfgId, string value)
        {
            var variant = "";
            var orderDescs = value.Split(new []{ ' ', ':', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(I => I.Trim());
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
            return variant;
        }
        public static List<ACG_CK.Order> GetCKOrder(orders order)
        {
            List<ACG_CK.Order> ords = new List<Order>();
            foreach (var o in order.order_items)
            {
                if (o.shipment_id > 0 || o.itemid == "111111")
                {
                    continue;
                }
                if (o.Product == null)
                {
                    orderItems.Add(o);
                    continue;
                }

                var orderDescs = o.itemdescription.Split(new []{ ' ', '\r', '\n', ':', ';', '\t' },StringSplitOptions.RemoveEmptyEntries).Select(I => I.Trim());
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
                if (variant == string.Empty)
                {
                    orderItems.Add(o);
                    continue;
                }
                if (variant == "universal")
                {
                    variant = string.Empty;
                }
                DateTime result = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd"), "yyyy-MM-dd", CultureInfo.InvariantCulture);

                ords.Add(new Order()
                {
                    CK_Item = o.Product.mfgid,
                    CK_SKU = o.Product.SKU,
                    CK_Variant = variant,
                    Comment = order.cus_comment,
                    PO = order.orderno,
                    PO_Date = DateTime.UtcNow.Date,
                    Qty = Convert.ToInt32(o.numitems),
                    Ship_Addr = order.shipaddress,
                    Ship_Addr_2 = order.shipaddress2,
                    Ship_City = order.shipcity,
                    Ship_Company = order.shipcompany.Substring(0, 25),
                    Ship_Country = order.shipcountry,
                    Ship_Email = order.shipemail,
                    Ship_Name = order.shipfirstname + " " + order.shiplastname,
                    Ship_Phone = order.shipphone,
                    Ship_Service = "R02",
                    Ship_State = order.shipstate,
                    Ship_Zip = order.shipzip
                });
            }
            return ords;
        }

        static ConfigurationData GetConfigurationDetails()
        {
            return new ConfigurationData
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["mysqlconnection"].ConnectionString,
                PrivateKey = ConfigurationManager.AppSettings["PrivateKey"],
                Store = ConfigurationManager.AppSettings["Store"],
                Token = ConfigurationManager.AppSettings["Token"],
                AuthUserName = ConfigurationManager.AppSettings["AuthUserName"],
                AuthPassowrd = ConfigurationManager.AppSettings["AuthPassowrd"]
            };
        } 
    }
}
