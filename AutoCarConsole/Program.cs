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
            //  CustomerDAL.AddCustomer(config);
            // ProductDAL.AddProduct(config);
            CsvFilebase baseCSBV = new CsvFilebase();
            string filePath =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string coverKingJobberFilePath = Path.Combine(filePath, "../../CoverKingData/ACG92_Custom Floormats_11112017.csv");
            var jData = baseCSBV.Read<JobberData>(coverKingJobberFilePath, true);
            foreach (var item in jData)
            {
                CoverKingDAL.Save(config.ConnectionString, item);
            }

            string coverKingAppDataFilePath = Path.Combine(filePath, "../../CoverKingData/ACG92_11112017.csv");
            var appData = baseCSBV.Read<AppData>(coverKingAppDataFilePath, true, new Dictionary<string, string>()
            {
                {"ItemOrSKU", "Item/SKU" },
                { "UPC_Code", "UPC Code" },
                { "Gross_Weight", "Gross Weight" },
                { "Product_Family_ID", "ProductFamily ID" },
                { "Product_Family_Description", "Product Family Description" },
            });
            foreach (var item in appData)
            {
                CoverKingDAL.Save(config.ConnectionString, item);
            }

            string amazonVariant = Path.Combine(filePath, "../../CoverKingData/20171110-ItmVr-As.txt");
            var amazonVariantData = baseCSBV.Read<AmazonVariant>(amazonVariant, true, new Dictionary<string, string>()
            {
                {"SKUOrUPC", "SKU/UPC" },
            });
            foreach (var item in amazonVariantData)
            {
                CoverKingDAL.Save(config.ConnectionString, item);
            }
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
                            oldLine = line;
                            continue;
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