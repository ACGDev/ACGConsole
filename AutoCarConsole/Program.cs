﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
 using System.IO;
 using System.Linq;
 using System.Net;
 using System.ServiceModel;
using System.Threading;
using AutoCarConsole.ACG_CK;
using AutoCarConsole.DAL;
using AutoCarConsole.Model;
using DCartRestAPIClient;
using MySql;
using MySql.Data.MySqlClient;
using System.Text;
/* using Mandrill;
 using Mandrill.Model;*/
 using Order = AutoCarConsole.ACG_CK.Order;

namespace AutoCarConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            ConfigurationData config = GetConfigurationDetails();
            // Uncomment following to get Customer records
            //CustomerDAL.AddCustomer(config);

            // Get first record that is not shipped or cancelled in the last 6 months. FetchDate=false will limit it to 3 days

            // SM: Why do we need two calls ? AddOrders and FetchOrders seem to be returning the exact same set of orders ??
            // why not List<orders> ordersDB = OrderDAL.AddOrders(config, false);   ??
            // SM: OrderDAL.AddOrders(config, false);
            // SM: List<orders> ordersDB = OrderDAL.FetchOrders(config.ConnectionString, false);

            List<orders> ordersDB = OrderDAL.SyncOrders(config, true);

            // Create ACG-yyyyMMDDHHMM.csv for uploading
            string fileName = string.Format("ACG-{0}.csv", DateTime.Now.ToString("yyyyMMMdd-HHmm"));
            string filePath =
               System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string strFileNameWithPath = string.Format("{0}\\{1}", filePath,
                fileName);

            string strCsvHeader = "PO,PO_Date,Ship_Company,Ship_Name,Ship_Addr,Ship_Addr_2,Ship_City,Ship_State,Ship_Zip,Ship_Country,Ship_Phone,Ship_Email,Ship_Service,CK_SKU,CK_Item,CK_Variant,Customized_Code,Customized_Msg,Customized_Code2,Customized_Msg2,Qty,Comment";
            File.WriteAllText(strFileNameWithPath, strCsvHeader + "\r\n");

            int nOrderLineCount = 0;

            foreach (var order in ordersDB)
            {
                // manually modify order if needed
                //if (order.orderno.Contains("161968"))
                //    order.orderno += "-6";
                // RestHelper.Execute(@"http://api.coverking.com/orders/Order_Placement.asmx?op=Place_Orders", config.AuthUserName, config.AuthPassowrd, order);
                string strOrderLines = RestHelper.GenerateOrderLines(order, config.AuthUserName, config.AuthPassowrd);
                if (strOrderLines != string.Empty)
                {
                    File.AppendAllText(strFileNameWithPath, strOrderLines);
                    nOrderLineCount++;
                }
            }
            // SM: Need to upload file to ftp. Try with any ftp first.
            if (nOrderLineCount>0)
            {
                // Ftp upload code here
                UploadFile(config, filePath, fileName);
            }
            OrderDAL.UpdateStatus(ordersDB);
        }

        public static void UploadFile(ConfigurationData config, string filePath, string fileName)
        {
            var ftAddress = config.FTPAddress + fileName;
            //FtpWebRequest request = (FtpWebRequest)WebRequest.Create(config.FTPAddress);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftAddress);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // This example assumes the FTP site uses anonymous logon.  
            request.Credentials = new NetworkCredential(config.FTPUserName, config.FTPPassword);

            // Copy the contents of the file to the request stream.  
            request.UsePassive = true;

            StreamReader sourceStream = new StreamReader(filePath + "\\" + fileName);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);

            response.Close();
        }
        /// <summary>
        /// todo: not used anywhere
        /// </summary>
        /// <param name="configData"></param>
        public static void SendEmail(ConfigurationData configData)
        {
            /*
             * Uncomment namespace from top
             * <add key="MandrilAPIKey" value="fake"/>
             */
            /*var api = new MandrillApi(configData.MandrilAPIKey);
            var message = new MandrillMessage("billing@autocareguys.com", "sample@gmail.com",
                "hello mandrill!", "...how are you?");
            var result = api.Messages.SendAsync(message);
            result.Wait();
            var checkResult = result.Result;*/
        }
        static ConfigurationData GetConfigurationDetails()
        {
            /*
                   <add key="FTPAddress" value="fake"/>
                   <add key="FTPUserName" value="fake"/>
                   <add key="FTPPassword" value="fake"/>
             */
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
                MandrilAPIKey = ConfigurationManager.AppSettings["MandrilAPIKey"]
            };
        } 
    }
}