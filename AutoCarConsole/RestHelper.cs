using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AutoCarConsole.Model;
using DCartRestAPIClient;

namespace AutoCarConsole
{
    public class RestHelper
    {
        public static List<order_items> orderItems;
        public static List<T> GetRestAPIRecords<T>(string strRestAPIID, string RestAPIType, string PrivateKey, string Token, string SecureURL, string RecordsGetLimit, long RecordsOffset, string StartDate = "")
        {
            List<T> strRestJSON = GetRestAPIJSON<T>(strRestAPIID, RestAPIType, PrivateKey, Token, SecureURL, RecordsGetLimit, RecordsOffset, StartDate);

            return strRestJSON;
        }

        private static List<T> GetRestAPIJSON<T>(string strRestAPIClientID, string restAPIClientType, string PrivateKey, string Token, string SecureURL, string RecordsGetLimit, long RecordsOffset, string StartDate = "")
        {
            RestAPIActions restAPIClientWM = GetRestAPIClient(PrivateKey, Token, SecureURL);

            string restAPIClientStream = string.Empty;
            List<T> reclist = new List<T>();
            restAPIClientWM.Type = restAPIClientType;

            int parsedValue;
            if (strRestAPIClientID.Length != 0 && int.TryParse(strRestAPIClientID, out parsedValue))
            {
                restAPIClientWM.ID = Convert.ToInt32(strRestAPIClientID);
            }

            int parsedRecordsGetLimitValue;
            if (RecordsGetLimit.Length != 0 && int.TryParse(RecordsGetLimit, out parsedRecordsGetLimitValue))
            {
                restAPIClientWM.RecordsGetLimit = Convert.ToInt32(RecordsGetLimit);
            }
            restAPIClientWM.RecordsOffset = RecordsOffset;
            if (StartDate != string.Empty)
                restAPIClientWM.datestart = StartDate;

            RecordInfo recInf = restAPIClientWM.GetRecords<T>();



            if (recInf.Status == ActionStatus.Succeeded)
            {

                try
                {
                    reclist = (List<T>)recInf.ResultSet;
                    restAPIClientStream = Newtonsoft.Json.JsonConvert.SerializeObject(reclist);
                }
                catch (Exception ex)
                {

                }

            }

            return reclist;
        }
        public static RestAPIActions GetRestAPIClient(string strPrivateKey, string strToken, string strSecureURL)
        {
            RestAPIActions restAPIClientWM = new RestAPIActions();

            string sHost = string.Empty;
            string sVersion = string.Empty;
            string sContentType = string.Empty;

            //Provide the parameters for the HTTP Client 
            sHost = "http://apirest.3dcart.com/3dCartWebAPI/v";
            sVersion = "1";
            sContentType = "application/json";

            restAPIClientWM.HttpHost = sHost;
            restAPIClientWM.ServiceVersion = sVersion;
            restAPIClientWM.PrivateKey = strPrivateKey;
            restAPIClientWM.Token = strToken;
            restAPIClientWM.SecureURL = strSecureURL;
            restAPIClientWM.ContentType = sContentType;

            return restAPIClientWM;
        }

        public static string GenerateOrderLines(orders order, ConfigurationData configData, Action<ConfigurationData, string, string> sendEmail)
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
                    variant = RestServiceCall.GetVariant(o.Product.mfgid, desc);
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

                // CK_Item,CK_Variant,Customized_Code,Customized_Msg,Customized_Code2,Customized_Msg2,Qty,Comment";
                oText += string.Format(",{0},{1},{2},{3},{4},{5},{6},{7}", o.Product.mfgid, variant, strMasterPakCode, strMasterPakCodeMsg, "", "",
                    o.numitems, order.cus_comment.Trim().Replace("\"", "&quot;"));

                orderFinal.AppendLine(oText);
                o.Product = null;
            }
            return orderFinal.ToString();
        }
    }
}