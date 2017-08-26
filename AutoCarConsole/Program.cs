using AutoCarOperations.Model;
using AutoCarOperations;
using AutoCarOperations.DAL;
using Mandrill;
using Mandrill.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using AutoCarConsole.ACG_CK;

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
            //ProductDAL.AddProduct(config);
            OrderDAL.PlaceOrder(config, true, false, false);
            
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