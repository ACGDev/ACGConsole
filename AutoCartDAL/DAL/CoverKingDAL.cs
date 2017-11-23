using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;

namespace AutoCarOperations.DAL
{
    public class CoverKingDAL
    {
        public static void Save(string connectionString, IEnumerable<IEnumerable<JobberData>> jobbers)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                context.Database.ExecuteSqlCommand("TRUNCATE ck_jobber;");

                foreach (var jobber in jobbers)
                {
                    context.CKJobber.AddRange(jobber);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
        public static void Save(string connectionString, IEnumerable<IEnumerable<AppData>> appData)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                context.Database.ExecuteSqlCommand("TRUNCATE ck_app_data;");
                foreach (var items in appData)
                {
                    context.CKAppData.AddRange(items);
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
                            var jobber = context.CKJobber.FirstOrDefault(I => I.ItemOrSKU == dup.NavItem);
                            if (jobber == null)
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
    }
}
