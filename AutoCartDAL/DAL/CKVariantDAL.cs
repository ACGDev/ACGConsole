using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using AutoCarOperations.Model;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static void SaveCKVariant(string connectionString, List<CKVariant> variants)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;

                foreach (var ck_var in variants)
                {
                    context.CKVaraints.AddOrUpdate(ck_var);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
    }
}
