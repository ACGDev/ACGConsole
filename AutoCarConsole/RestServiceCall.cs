using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using AutoCarConsole.ACG_CK;
using AutoCarConsole.Model;

namespace AutoCarConsole
{
    public static class RestServiceCall
    {
        public static List<order_items> orderItems = new List<order_items>();

        public static void PlaceOrder(ConfigurationData configData, List<orders> ordersDB)
        {
            foreach (var order in ordersDB)
            {
                var ckOrders = GetCKOrder(order);
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(ckOrders);
                if (ckOrders.Count > 0)
                {
                    Order[] oro = ckOrders.ToArray();
                    Order_PlacementSoapClient placeOrder = new Order_PlacementSoapClient("Order_PlacementSoap");
                    try
                    {
                        placeOrder.Open();
                        var response = placeOrder.Place_Orders(new Auth_Header()
                        {
                            DealerID = configData.AuthUserName,
                            Password = configData.AuthPassowrd
                        }, oro);
                    }
                    catch (FaultException e)
                    {
                        placeOrder.Close();
                        throw;
                    }
                }
            }
        }

        public static string GetVariant(string mfgId, string value)
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
                    Customized_Code2 = "",
                    Customized_Msg2 = ""
                });
            }
            return ords;
        }
    }
}
