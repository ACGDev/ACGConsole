using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarConsole.Model;
using DCartRestAPIClient;
using MySql.Data.MySqlClient;
using System.Data.Entity;

namespace AutoCarConsole.DAL
{
    public static class OrderDAL
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="fetchDate"></param>
        /// <returns></returns>
        public static List<orders> FetchOrders(string connectionString, bool fetchDate)
        {
            Console.WriteLine("..........Fetch Orders..........");
            var strOrderStart = FetchLastOrderDate(connectionString, fetchDate);
            List<orders> ordersDB;
            using (var context = new AutoCareDataContext())
            {
                //ordersDB = context.Orders.Include(I => I.order_items).Include("order_items.Product").Where(I => I.shipcomplete != "shipped" && I.shipcomplete != "Cancelled").ToList();
                DateTime dt = Convert.ToDateTime(strOrderStart);
                ordersDB = context.Orders.Include(I => I.order_items).Include("order_items.Product").Where(I => I.orderdate >= dt).ToList();
            }
            Console.WriteLine("..........Finished..........");
            return ordersDB;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="fetchDate"></param>
        /// <returns></returns>
        public static List<orders> AddOrders(ConfigurationData config, bool fetchDate)
        {
            var strOrderStart = FetchLastOrderDate(config.ConnectionString, fetchDate);
            List<DCartRestAPIClient.Order> orders = new List<DCartRestAPIClient.Order>();
            var skip = 0;
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
            var ordersDb = AddOrders(orders);
            return ordersDb;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        private static List<orders> AddOrders(List<Order> orders)
        {
            var orderDB = GetOrders(orders);
            using (var context = new AutoCareDataContext())
            {
                bool flag = false;
                foreach (var order in orderDB)
                {
                    bool bAddthisOrder = false;
                    // -- status: 4 - shipped, 5 - Cancelled, 7 - incomplete, 1 - new
                    if (order.order_status == 7)
                    {
                        continue;
                    }
                    var record = context.Orders.FirstOrDefault(I => I.order_id == order.order_id);
                    // Handle if new order or not shipped or cancelled 
                    if (record == null)
                        bAddthisOrder = true;
                    else if (order.order_status == 5 && record.order_status != 5)  // if previously shipped, it can be cancelled now
                        bAddthisOrder = true;
                    else  if ( record.order_status != 5) // skip if previously cancelled
                        bAddthisOrder = true;

                    if (bAddthisOrder)
                    {
                        if (order.referer.Length > 98)
                        {
                            order.referer = order.referer.Substring(0, 98);
                            Console.WriteLine("referer "+order.referer+" length "+order.referer.Length.ToString());
                        }
                        context.Orders.AddOrUpdate(order);
                        flag = true;

                    }

                }
                if (flag)
                {
                    context.SaveChanges();
                }
            }
            return orderDB;
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
        /// Convert REST Order List to DB orders
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        private static List<orders> GetOrders(List<Order> orders)
        {
            List<orders> dbOrders = new List<orders>();
            foreach (var order in orders)
            {
                var orderKeyDict = order.ContinueURL.Split('&').Select(q => q.Split('='))
                    .ToDictionary(k => k[0], v => v[1]);
                var orderShipMents = order.ShipmentList[0];
                if (order.OrderStatusID == 7)
                    continue;
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
                    other2 = "",
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
                    int nItemsShipped = 0;
                    foreach (var shiprow in thisOrder.order_shipments)
                    {
                        if (shiprow.oshippeddate != "")
                            nItemsShipped++;
                    }
                    if (nItemsShipped == thisOrder.order_shipments.Count)
                        thisOrder.shipcomplete = "Shipped";
                    else if (nItemsShipped > 0)
                        thisOrder.shipcomplete = string.Format("Partial {0}/{1}", nItemsShipped, thisOrder.order_items.Count);
                }


                dbOrders.Add(thisOrder);
            }
            return dbOrders;
        }
        /// <summary>
        ///     To fetch ALL orders comment pass <para>fetchLastOrderDate</para> false
        /// </summary>
        /// <param name="myConnectionString"></param>
        /// <param name="fetchDate"></param>
        /// <returns></returns>
        private static string FetchLastOrderDate(string myConnectionString, bool fetchDate)
        {
            string strOrderStart = DateTime.Today.AddDays(-3).ToString("MM/dd/yyyy");
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
                        "select orderdate from orders where Shipcomplete = 'Pending' order by orderdate limit 1";
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
    }
}