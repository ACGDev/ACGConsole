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
    }
}