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
            List<Customer> customers = new List<Customer>();
            var skip = 0;
            //  Uncomment this to get Customer records 

            var customerGroups = RestHelper.GetRestAPIRecords<CustomerGroup>("", "CustomerGroups", config.PrivateKey, config.Token, config.Store, "100", 0, "");
            while (true)
            {
                var records = RestHelper.GetRestAPIRecords<Customer>("", "Customers", config.PrivateKey, config.Token, config.Store, "200", skip, "");
                int counter = records.Count;
                Console.WriteLine("..........Fetches " + counter + " Customer Record..........");
                customers.AddRange(records);
                CustomerDAL custDAL = new CustomerDAL();
                custDAL.AddCustomers(customers, customerGroups);
                if (counter < 200)
                {
                    break;
                }
                skip = 201 + skip;

                // customers.AddRange(records);
            }
            

            skip = 0;
            string strOrderStart = DateTime.Today.AddDays(-1).ToString("MM/dd/yyyy");
            // Get first record that is not shipped or cancelled in the last 6 months
            // To fetch ALL orders comment out the following portion

            /**
            MySql.Data.MySqlClient.MySqlConnection conn;
            string myConnectionString = ConfigurationManager.ConnectionStrings["mysqlconnection"].ToString();
            try
            {
                using (conn = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString))
                {

                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select orderdate from orders where Shipcomplete = 'Pending' order by orderdate limit 1";
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
                return;
            }
            **/

            Console.WriteLine("..........Fetch Orders..........");
            List<DCartRestAPIClient.Order> orders = new List<DCartRestAPIClient.Order>();

            while (true)
            {
                var records = RestHelper.GetRestAPIRecords<DCartRestAPIClient.Order>("", "Orders", config.PrivateKey, config.Token, config.Store, "100", skip, strOrderStart);
                int counter = records.Count;
                Console.WriteLine("..........Fetches " + counter + " Order Record..........");
                orders.AddRange(records);
                if (counter < 100)
                {
                    break;
                }

                skip = 101 + skip;


            }
            OrderDAL odDAL = new OrderDAL();
            var ordersDb = odDAL.AddOrders(orders);

            List<orders> ordersDB;
            using (var context = new AutoCareDataContext())
            {
                //ordersDB = context.Orders.Include(I => I.order_items).Include("order_items.Product").Where(I => I.shipcomplete != "shipped" && I.shipcomplete != "Cancelled").ToList();
                DateTime dt = Convert.ToDateTime(strOrderStart);
                ordersDB = context.Orders.Include(I => I.order_items).Include("order_items.Product").Where(I => I.orderdate >= dt).ToList();
            }
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            foreach (var order in ordersDB)
            {

                //var ckOrders = GetCKOrder(order);
                //var json = Newtonsoft.Json.JsonConvert.SerializeObject(ckOrders);
                //if (ckOrders.Count > 0)
                //{
                //    Order[] oro = ckOrders.ToArray();
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
                RestHelper.Execute(@"http://api.coverking.com/orders/Order_Placement.asmx?op=Place_Orders", config.AuthUserName, config.AuthPassowrd, order);

            }
        }

        public static string GetVariant(string mfgId, string value)
        {
            var variant = "";
            var orderDescs = value.Split(new []{ ' ', ':', ';','<' }, StringSplitOptions.RemoveEmptyEntries).Select(I => I.Trim());
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
            if (variant.Substring(0, 1) == "-")
                variant = variant.Substring(1);
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
                String shipcompany = order.shipcompany.Trim();
                if (shipcompany.Length > 25)
                    shipcompany = shipcompany.Substring(0, 25);
                ords.Add(new Order()
                {
                    CK_Item = o.Product.mfgid,
                    CK_SKU = "",
                    CK_Variant = variant,
                    Comment = order.cus_comment,
                    PO = order.orderno,
                    PO_Date = DateTime.UtcNow.Date,
                    Qty = Convert.ToInt32(o.numitems),
                    Ship_Addr = order.shipaddress,
                    Ship_Addr_2 = order.shipaddress2,
                    Ship_City = order.shipcity,
                    Ship_Company = shipcompany,
                    Ship_Country = order.shipcountry,
                    Ship_Email = order.shipemail,
                    Ship_Name = order.shipfirstname + " " + order.shiplastname,
                    Ship_Phone = order.shipphone,
                    Ship_Service = "R02",
                    Ship_State = order.shipstate,
                    Ship_Zip = order.shipzip,
                    Customized_Code = "",
                    Customized_Msg = "",
                    Customized_Code2="",
                    Customized_Msg2="",


                });
            }
            /*
             PO	Your purchase order number			18 characters max
            PO_Date	Purchase Order Date			
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
            Ship_Service	Service to be used eg. Ground, 2-Day, Next Day etc…)			See other Ship_Service_Code Worksheet
            CK_SKU	Coverking SKU as returned at the time of part look up eg: (539200-CSCV8, 538792-CVC6SP98). Also you can use Coverking Items numbers like M1 for Lock and cable etc as applicable			
            CK_Item	If not using the partlookup then put the Coverking Item number here			
            CK_Variant	Coverking Item application numbers ( CH118 or CH2464 etc. )			
            Customized_Code	Customization codes for Logo's or Embroidery			
            Customized_Msg	The Embroidery message that will go with the previous column. Limit 15 charecters			
            Customized_Code2	Second Logo or Embroidery for the item			
            Customized_Msg2	Second Embroidery Message that will go the code			
            Qty	Quantity for this item			
            Comment	Notes on this order item.			35 characters max

             * */
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
