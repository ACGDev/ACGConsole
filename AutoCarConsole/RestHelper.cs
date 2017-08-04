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

        /** Execute() is not used. Delete later**/
        public static void Execute(string url, string userName, string password, orders order)
        {
            HttpWebRequest request = CreateWebRequest(url);
            XmlDocument soapEnvelopeXml = new XmlDocument();
            var orderSOAP = GenerateOrder(order);
            if (orderSOAP == string.Empty)
            {
                return;
            }

            soapEnvelopeXml.LoadXml(string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
                    <soap12:Header>
                        <Auth_Header xmlns=""http://api.coverking.com/Orders"">
                            <DealerID>{0}</DealerID>
                            <Password>{1}</Password>
                        </Auth_Header>
                    </soap12:Header>
                    <soap12:Body>
                        <Place_Orders xmlns=""http://api.coverking.com/Orders"">
                            <Orders>" +
                                                  orderSOAP +
                                                  @"</Orders>
                        </Place_Orders>
                    </soap12:Body>
                </soap12:Envelope>", userName, password));

            using (Stream stream = request.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }

            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                {
                    string soapResult = rd.ReadToEnd();
                    Console.WriteLine(soapResult);
                }
            }
        }
        /** GenerateOrder() is not used. Delete later**/
        public static string GenerateOrder(orders order)
        {
            string orderFinal = string.Empty;
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
                    variant = RestServiceCall.GetVariant(o.Product.mfgid, desc);
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
                //string s = reader.ReadContentAsString();
                //DateTime when = DateTime.ParseExact(s, "M/d/yy hh:mm tt",
                //    CultureInfo.InvariantCulture);
                order.shipcompany = order.shipcompany.Trim();
                if (order.shipcompany.Length > 25)
                    order.shipcompany = order.shipcompany.Substring(0, 25).Trim();

                if (order.shipcompany.Length > 25)
                    order.shipcompany = order.shipcompany.Substring(0, 25).Trim();
                // CK_SKU should be blank when Mfg ID and Variant are present.

                // ** NEED to incorporate length for all fields. See Progarm.cs, line 240

                /*
                string orderText = string.Format(@"<Order>
                                                        <PO>{0}</PO>
                                                        <PO_Date>{1}</PO_Date>
                                                        <Ship_Company>{2}</Ship_Company>
                                                        <Ship_Name>{3}</Ship_Name>
                                                        <Ship_Addr>{4}</Ship_Addr>
                                                        <Ship_Addr_2>{5}</Ship_Addr_2>
                                                        <Ship_City>{6}</Ship_City>
                                                        <Ship_State>{7}</Ship_State>
                                                        <Ship_Zip>{8}</Ship_Zip>
                                                        <Ship_Country>{9}</Ship_Country>
                                                        <Ship_Phone>{10}</Ship_Phone>
                                                        <Ship_Email>{11}</Ship_Email>
                                                        <Ship_Service>{12}</Ship_Service>
                                                        <CK_SKU>{13}</CK_SKU>
                                                        <CK_Item>{14}</CK_Item>
                                                        <CK_Variant>{15}</CK_Variant>
                                                        <Customized_Code>{16}</Customized_Code>
                                                        <Customized_Msg>{17}</Customized_Msg>
                                                        <Customized_Code2>{18}</Customized_Code2>
                                                        <Customized_Msg2>{19}</Customized_Msg2>
                                                        <Qty>{20}</Qty>
                                                        <Comment>{21}</Comment>
                                                    </Order>", order.orderno,
                    DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"), order.shipcompany, (order.shipfirstname +" "+ order.shiplastname), order.shipaddress,
                    order.shipaddress2, order.shipcity, order.shipstate, order.shipzip, order.shipcountry,
                    order.shipphone, order.shipemail, "R02", "",
                    o.Product.mfgid, variant, strMasterPakCode, strMasterPakCodeMsg, " ", " ", o.numitems,
                    order.cus_comment);
                */
                string oText = string.Format("<Order><PO>{0}</PO><PO_Date>{1}</PO_Date>",
                    order.orderno, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                // if (order.shipcompany.Trim() != string.Empty)
                oText += string.Format("<Ship_Company>{0}</Ship_Company>", order.shipcompany);
                oText += string.Format("<Ship_Name>{0}</Ship_Name>", order.shipfirstname.Trim() + " " + order.shiplastname.Trim());
                oText += string.Format("<Ship_Addr>{0}</Ship_Addr>", order.shipaddress.Trim());
                //if (order.shipaddress2.Trim() != string.Empty)
                oText += string.Format("<Ship_Addr_2>{0}</Ship_Addr_2>", order.shipaddress2.Trim());

                // MUST have state. If not, set it same as Country
                if (order.shipstate.Trim() == string.Empty)
                    order.shipstate = order.shipcountry;
                // MUST have zip. If not, put 0
                if (order.shipzip.Trim() == string.Empty)
                    order.shipzip = "0";

                oText += string.Format("<Ship_City>{0}</Ship_City><Ship_State>{1}</Ship_State><Ship_Zip>{2}</Ship_Zip><Ship_Country>{3}</Ship_Country>",
                    order.shipcity.Trim(), order.shipstate.Trim(), order.shipzip.Trim(), order.shipcountry.Trim());

                //  if (order.shipphone.Trim() != string.Empty)
                oText += string.Format("<Ship_Phone>{0}</Ship_Phone>", order.shipphone.Trim());

                //  if (order.shipemail.Trim() != string.Empty)
                oText += string.Format("<Ship_Email>{0}</Ship_Email>", order.shipemail.Trim());

                oText += string.Format("<Ship_Service>{0}</Ship_Service>", "R02");

                // DON't need to send CK_SKU
                //if (order.sku.Trim() != string.Empty)
                //    oText += string.Format("<CK_SKU>{0}</CK_SKU>", order.sku.Trim());

                oText += string.Format("<CK_Item>{0}</CK_Item><CK_Variant>{1}</CK_Variant>", o.Product.mfgid, variant);
                if (strMasterPakCode != string.Empty)
                    oText += string.Format("<Customized_Code>{0}</Customized_Code><Customized_Msg>{1}</Customized_Msg>",
                        strMasterPakCode, strMasterPakCodeMsg);

                oText += string.Format("<Qty>{0}</Qty>", o.numitems);
                // if (order.cus_comment.Trim() != string.Empty)
                oText += string.Format("<Comment>{0}</Comment>", order.cus_comment.Trim());

                oText += "</Order>";

                orderFinal = orderFinal + "\n" + oText;
            }
            return orderFinal;
        }

        /// <summary>
        /// Create a soap webrequest to [Url]
        /// Not used for file upload
        /// </summary>
        /// <returns></returns>
        public static HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", "http://api.coverking.com/Orders/Place_Orders");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }
        public static string GenerateOrderLines(orders order, string userName, string password)
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
            }
            return orderFinal.ToString();
        }
    }
}