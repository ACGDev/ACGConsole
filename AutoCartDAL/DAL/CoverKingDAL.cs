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
        public static void Save(string connectionString, IEnumerable<JobberData> jobbers)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                foreach (var jobber in jobbers)
                {
                    context.CKJobber.AddOrUpdate(jobber);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
        public static void Save(string connectionString, IEnumerable<AppData> appData)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;

                foreach (var app in appData)
                {
                    context.CKAppData.AddOrUpdate(app);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
        public static void Save(string connectionString, IEnumerable<AmazonVariant> amazonVariants)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;

                //foreach (var app in amazonVariants)
                {
                    context.CKAmazonVariant.AddRange(amazonVariants);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;

                //var data = context.Database.SqlQuery<string>(
                //    "SELECT ASIN FROM car.ck_temp_amazon_variant GROUP BY ASIN HAVING COUNT(1) >1").ToList();
                //if (data.Count > 0)
                //{
                //    foreach (var d in data)
                //    {
                //        context.CKAmazonVariant.Remove()
                //    }
                //}
            }
        }
    }
}
