using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using AutoCarOperations.Model;

namespace AutoCarOperations.DAL
{
    public class CoverKingDAL
    {
        public static void Save(string connectionString, IEnumerable<IEnumerable<DownloadedItem>> downloadedItems)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                context.Database.ExecuteSqlCommand("TRUNCATE ck_jobber;");

                foreach (var item in downloadedItems)
                {
                    context.CKDownloadedItem.AddRange(item);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
        public static void Save(string connectionString, IEnumerable<IEnumerable<DownloadVariant>> downloadedVariant)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                context.Database.ExecuteSqlCommand("TRUNCATE ck_app_data;");
                foreach (var items in downloadedVariant)
                {
                    context.CKDownloadedVariant.AddRange(items);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
        public static void Save(string connectionString, IEnumerable<IEnumerable<AmazonVariant>> amazonVariants)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                context.Database.ExecuteSqlCommand("TRUNCATE ck_amazon_variant;");

                foreach (var item in amazonVariants)
                {
                    var groupItem = item.GroupBy(i => i.ASIN).Select(j => j.First()).ToList();
                    context.CKAmazonVariant.AddRange(groupItem);
                    context.SaveChanges();
                }
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;

                var data = context.Database.SqlQuery<string>(
                    "SELECT ASIN FROM car.ck_temp_amazon_variant GROUP BY ASIN HAVING COUNT(1) >1").ToList();
                if (data.Count > 0)
                {
                    foreach (var d in data)
                    {
                        var dupObjs = context.CKAmazonVariant.Where(I => I.ASIN == d).ToList();
                        foreach (var dup in dupObjs)
                        {
                            var item = context.CKDownloadedItem.FirstOrDefault(I => I.ItemID == dup.NavItem);
                            if (item == null)
                            {
                                context.CKAmazonVariant.Remove(dup);
                            }
                        }
                        //context.CKAmazonVariant.Remove()
                    }
                }
                context.SaveChanges();
            }
        }

        public static void SaveProductOnJobber(string connectionString)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                var products = context.CKProductMaster.ToList();
                foreach (var p in products)
                {
                    var jobbers = context.CKDownloadedItem.Where(I => I.ItemID.StartsWith(p.product_id) && I.Product_Family_ID == p.product_family_id).ToList();
                    if (jobbers.Count > 0)
                    {
                        foreach (var jobber in jobbers)
                        {
                            context.CKDownloadedItem.Attach(jobber);
                            jobber.ProductID = p.product_id;
                            jobber.MaterialID = jobber.ItemID.Replace(p.product_id, "");
                            context.Entry(jobber).Property(I => I.ProductID).IsModified = true;
                            context.Entry(jobber).Property(I => I.MaterialID).IsModified = true;
                        }
                   }
                }
                context.SaveChanges();
            }
        }

        public static void SaveBaseVehicleAppData(string connectionString)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                var appData = context.CKDownloadedVariant.OrderBy(I => I.ProductID).ThenBy(I=> I.VariantID).ThenBy(I => I.SubModel).ToList();
                foreach (var a in appData)
                {
                    context.CKDownloadedVariant.Attach(a);

                    var baseVehicleId =
                        CallService(
                            $"http://api.coverking.com/getData.asmx/GetSubmodel?Productid={a.ProductID}&Year={a.From_Year}&Make={a.Make_Descr}&Model={a.Model_Descr}",
                            "//ns:Submodel_table/ns:Basevehicle");
                    a.BaseVehicleID = baseVehicleId;

                    if (baseVehicleId != null)
                    {
                        var recID =
                            CallService(
                                $"http://api.coverking.com/getData.asmx/Get_options_gen?Basevehicle={baseVehicleId}&Productid={a.ProductID}&Submodel=&Customer_IP=223.223.154.114&DealerID=ACG92",
                                "//ns:ArrayOfOptions/ns:Options/ns:recid");
                        a.RecID= recID;
                        context.Entry(a).Property(I => I.RecID).IsModified = true;
                    }
                    context.Entry(a).Property(I => I.BaseVehicleID).IsModified = true;
                }
                context.SaveChanges();
            }
        }

        public static void SaveItemVariantImages(string connectionString)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                var products = context.CKProductMaster.ToList();
                foreach (var p in products)
                {
                    XmlNamespaceManager ns;
                    var results = CallService($"http://api.coverking.com/getData.asmx/Material_category_img_list?prod={p.product_id}&mcat=&DealerID=ACG92", out ns);
                    XmlNodeList xnl = results.SelectNodes("/ns:ArrayOfMaterial_category_img/ns:Material_category_img/ns:Swatches/ns:swatch", ns);
                    foreach (XmlNode node in xnl)
                    {
                        string materialId = node["MaterialID"].InnerText;
                        string imageURL = node["Image_URL"].InnerText;
                    }
                }
            }
        }

        public static XmlDocument CallService(string address, out XmlNamespaceManager ns)
        {
            var request = WebRequest.Create(address) as HttpWebRequest;
            using (var response = request.GetResponse())
            using (Stream receiveStream = response.GetResponseStream())
            using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
            {
                var result = readStream.ReadToEnd();
                XmlDocument results = new XmlDocument();
                results.LoadXml(result);

                ns = new XmlNamespaceManager(results.NameTable);
                ns.AddNamespace("ns",
                    "http://coverkingprod.cloudapp.net/");
                return results;
            }
        }

        public static string CallService(string address, string query)
        {
            XmlNamespaceManager ns;
            var selectSingleNode = CallService(address, out ns).SelectSingleNode(
                query, ns);
            return selectSingleNode?.InnerText;
        }
    }
}
