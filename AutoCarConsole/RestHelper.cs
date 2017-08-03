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

        private static List<T> GetRestAPIJSON<T>(string strRestAPIClientID, string restAPIClientType, string PrivateKey, string Token, string SecureURL, string RecordsGetLimit, long RecordsOffset, string StartDate ="")
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
                                GenerateOrder(order) +
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
                    variant = Program.GetVariant(o.Product.mfgid, desc);
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
                    order.shipcompany = order.shipcompany.Substring(0, 25);

                // CK_SKU should be blank when Mfg ID and Variant are present.
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
                    DateTime.Now.ToString("MM/dd/yyyy"), order.shipcompany, (order.shipfirstname +" "+ order.shiplastname), order.shipaddress,
                    order.shipaddress2, order.shipcity, order.shipstate, order.shipzip, order.shipcountry,
                    order.shipphone, order.shipemail, "R02", "",
                    o.Product.mfgid, variant, strMasterPakCode, strMasterPakCodeMsg, string.Empty, string.Empty, o.numitems,
                    order.cus_comment);
                orderFinal = orderFinal + "\n" + orderText;
            }
            return orderFinal;
        }
        /// <summary>
        /// Create a soap webrequest to [Url]
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
    }
}
