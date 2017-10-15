using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using AutoCarOperations.Model;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace AutoCarOperations.DAL
{
    public class CKVariantDAL
    {
        public static CKVariant FindCKVariant(string connectionString, Func<CKVariant, bool> condFunc)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.CKVaraints.FirstOrDefault(condFunc);
            }
        } 
        public static void SaveCKVariant(string connectionString, List<TempCKVariant> variants)
        {
            var group_variant = variants.GroupBy(i => i.SKU).Select(j => j.First()).ToList();
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                
                foreach (var ck_var in group_variant)
                {
                    var this_set= context.TempCKVariants.FirstOrDefault(i => i.SKU == ck_var.SKU);
                    if (this_set == null)
                       context.TempCKVariants.AddOrUpdate(ck_var);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
        // deletes all records from temp variant table
        public static void DeleteCKVariant(string myConnectionString)
        {
            using (MySqlConnection conn = new MySqlConnection(myConnectionString))
            {

                MySqlCommand cmd = conn.CreateCommand();
                // 4: Shipped, 5: Cancelled
                cmd.CommandText =
                    "delete from ck_temp_itemvariant";
                
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}
