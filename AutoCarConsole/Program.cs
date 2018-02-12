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
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AutoCarConsole.ACG_CK;
using Newtonsoft.Json.Serialization;

namespace AutoCarConsole
{

    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            ConfigurationData config = GetConfigurationDetails();
            // Uncomment following to get Customer records
              CustomerDAL.AddCustomer(config);
            // ProductDAL.AddProduct(config);
            //CoverKingDAL.SaveBaseVehicleAppData(config.ConnectionString);
            //CoverKingDAL.SaveItemVariantImages(config.ConnectionString);
            CsvFilebase baseCSBV = new CsvFilebase();
            string filePath =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string coverkingPath = Path.Combine(filePath, "../../CoverKingData/");
            /*
            DirectoryInfo dirApp = new DirectoryInfo(Path.Combine(coverkingPath, "AppData/Incoming/"));
            foreach (var file in dirApp.GetFiles())
            {
                var aData = baseCSBV.Read<DownloadVariant>(file.FullName, true);
                CoverKingDAL.Save(config.ConnectionString, aData);
                file.MoveTo(Path.Combine(coverkingPath, "AppData/Processed/" + file.Name));
            }
            */
            DirectoryInfo dirJobber = new DirectoryInfo(Path.Combine(coverkingPath, "Jobber/Incoming/"));
            foreach (var file in dirJobber.GetFiles())
            {
                var jData = baseCSBV.Read<DownloadedItem>(file.FullName, true, new Dictionary<string, string>()
                {
                    {"ItemOrSKU", "Item/SKU" },
                    { "UPC_Code", "UPC Code" },
                    { "Gross_Weight", "Gross Weight" },
                    { "Product_Family_ID", "ProductFamily ID" },
                    { "Product_Family_Description", "Product Family Description" },
                });
                CoverKingDAL.Save(config.ConnectionString, jData);
                file.MoveTo(Path.Combine(coverkingPath, "Jobber/Processed/"+file.Name));
            }

            DirectoryInfo dirAmazon = new DirectoryInfo(Path.Combine(coverkingPath, "AmazonVariant/Incoming/"));
            foreach (var file in dirAmazon.GetFiles())
            {
                var amazonVariantData = baseCSBV.Read<AmazonVariant>(file.FullName, true,
                    new Dictionary<string, string>()
                    {
                        {"SKUOrUPC", "SKU/UPC"},
                    });
                CoverKingDAL.Save(config.ConnectionString, amazonVariantData);
                file.MoveTo(Path.Combine(coverkingPath, "Jobber/Processed/" + file.Name));
            }
            CoverKingDAL.SaveProductOnJobber(config.ConnectionString);

            //OrderDAL.PlaceOrder(config, true, true, true);
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

    class CsvFilebase
    {
        public virtual void AssignValuesFromCsv(object obj, string[] propertyValues, string[] headerNames, Dictionary<string, string> fieldMap)
        {
            var properties = obj.GetType().GetProperties();
            for (var i = 0; i < properties.Length; i++)
            {
                string name = properties[i].Name;
                if (fieldMap != null)
                {
                    name = fieldMap.ContainsKey(name) ? fieldMap[name] : name;
                }
                var index = Array.FindIndex(headerNames, K => K.Contains(name));
                if (index == -1)
                {
                    continue;
                }
                var type = properties[i].PropertyType.Name;
                switch (type)
                {
                    case "Int32":
                        properties[i].SetValue(obj, int.Parse(propertyValues[index]));
                        break;
                    case "Nullable`1":
                        if (properties[i].PropertyType == typeof(decimal?))
                        {
                            properties[i].SetValue(obj, decimal.Parse(propertyValues[index]));
                        }
                        else
                        {
                            properties[i].SetValue(obj, double.Parse(propertyValues[index]));
                        }
                        break;
                    default:
                        if (propertyValues[index] == "\"\"" || propertyValues[index] == "NULL")
                        {
                            properties[i].SetValue(obj, null);
                        }
                        else
                        {
                            properties[i].SetValue(obj, propertyValues[index]);
                        }
                        break;
                }
            }
        }

        public IEnumerable<IEnumerable<T>> Read<T>(string filePath, bool hasHeaders, Dictionary<string,string> fieldMap = null) where T: class, new()
        {
            var records = new List<List<T>>();
            var objects = new List<T>();
            string[] headerNames = null;
            string oldLine = string.Empty;
            using (var sr = new StreamReader(filePath))
            {
                bool headersRead = false;
                string line;
                int i = 0;
                do
                {
                    line = sr.ReadLine();
                    if (line != null && headersRead)
                    {
                        var obj = new T();
                        if (oldLine != string.Empty)
                        {
                            line = oldLine.Replace(Environment.NewLine, "") + line;
                        }
                        var propertyValues = line.Split(new[] {',', '\t'});
                        if (propertyValues.Length != headerNames.Length)
                        {
                            var result =  Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                            if (result.Length != headerNames.Length)
                            {
                                oldLine = line;
                                continue;
                            }
                            else
                            {
                                propertyValues = result;
                            }
                        }
                        oldLine = string.Empty;
                        AssignValuesFromCsv(obj, propertyValues, headerNames, fieldMap);
                        objects.Add(obj);
                    }
                    if (!headersRead)
                    {
                        headerNames = line.Split(new[] { ',', '\t' });
                        headersRead = true;
                    }
                    if (i == 1000)
                    {
                        records.Add(objects);
                        i = 0;
                        objects = new List<T>();
                    }
                    i = i + 1;
                } while (line != null);

                if (objects.Count > 0)
                {
                    records.Add(objects);
                }
            }
            return records;
        }
    }
}