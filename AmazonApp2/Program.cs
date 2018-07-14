using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp2
{
    class Program
    {
        static void Main(string[] args)
        {

            /* Example Products API Client */

        
        }
    }

    static class ConfigurationData
    {
        public static string AccessKey = ConfigurationManager.AppSettings["AccessKey"];
        public static string SecretKey = ConfigurationManager.AppSettings["SecretKey"];
        public static string AppName = ConfigurationManager.AppSettings["AppName"];
        public static string Version = ConfigurationManager.AppSettings["Version"];
        public static string ServiceURL = ConfigurationManager.AppSettings["ServiceURL"];
        public static string SellerId = ConfigurationManager.AppSettings["SellerId"];
        public static string MWSToken = ConfigurationManager.AppSettings["MWSToken"];
        public static string MarketId1 = ConfigurationManager.AppSettings["MarketId1"];
        public static string MarketId2 = ConfigurationManager.AppSettings["MarketId2"];
        public static string MarketId3 = ConfigurationManager.AppSettings["MarketId3"];
        public static string ConnectionString = ConfigurationManager.ConnectionStrings["mysqlconnection"].ConnectionString;
        internal static string PrivateKey = ConfigurationManager.AppSettings["PrivateKey"];
        internal static string Token = ConfigurationManager.AppSettings["Token"];
        internal static string FTPAddress = ConfigurationManager.AppSettings["FTPAddress"];
        internal static string FTPUserName = ConfigurationManager.AppSettings["FTPUserName"];
        internal static string FTPPassword = ConfigurationManager.AppSettings["FTPPassword"];
        internal static string Store = ConfigurationManager.AppSettings["Store"];
        internal static string CKOrderFolder = ConfigurationManager.AppSettings["CKOrderFolder"];
        internal static string MandrilAPIKey = ConfigurationManager.AppSettings["MandrilAPIKey"];
    }

}
