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
        public static void Save<T>(string connectionString, IEnumerable<T> jobbers) where T: class
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;

                foreach (var jobber in jobbers)
                {
                    context.Set<T>().AddOrUpdate(jobber);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
    }
}
