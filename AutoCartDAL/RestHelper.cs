using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AutoCarOperations.Model;
using DCartRestAPIClient;
using Newtonsoft.Json;

namespace AutoCarOperations
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
        public static RecordInfo AddRecord(Object RestAPIObject, string RestAPIType, string PrivateKey, string Token, string SecureURL)
        {
            RestAPIActions restAPIClientWM = GetRestAPIClient(PrivateKey, Token, SecureURL);

            restAPIClientWM.Type = RestAPIType;

            RecordInfo addInfo = restAPIClientWM.AddRecord(RestAPIObject);
            return addInfo;
            //string sIDPrefix = RestAPIType.Substring(0, RestAPIType.Length - 1);

            //if (addInfo.Status == ActionStatus.Succeeded)
            //{
            //    return sIDPrefix + " added. " + sIDPrefix + "ID: " + addInfo.ResultSet;

            //}

            //else
            //{
            //    return sIDPrefix + " add failed.  Error Code: " + addInfo.CodeNumber + ", Description: " + addInfo.Description;
            //}


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
    }
}