﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCarOperations.Model
{
    public class ConfigurationData
    {
        public string PrivateKey { get; set; }
        public string Store { get; set; }
        public string Token { get; set; }
        public string ConnectionString { get; set; }
        public string AuthUserName { get; set; }
        public string AuthPassowrd { get; set; }
        public string FTPAddress { get; set; }
        public string FTPUserName { get; set; }
        public string FTPPassword { get; set; }
        public string MandrilAPIKey { get; set; }
        public string JFWFTPAddress { get; set; }
        public string JFWFTPUserName { get; set; }
        public string JFWFTPPassword { get; set; }
        public string CoverKingAPIKey { get; set; }
        public string _3dCartFTPAddress { get; set; }
        public string _3dCartFTPUserName { get; set; }
        public string _3dCartFTPPassword { get; set; }
        public string CKOrderFolder { get; set; }
        public string NewJFWOrderStatusID { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Version { get; set; }
        public string AppName { get; set; }
        public string ServiceURL { get; set; }
        public string SellerId { get; set; }
        public string MWSToken { get; set; }
    }
}
