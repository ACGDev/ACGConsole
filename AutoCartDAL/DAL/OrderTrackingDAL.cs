using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;

namespace AutoCarOperations.DAL
{
    public class OrderTrackingDAL
    {
        public static void SaveOrderTracking(string connectionString, List<order_tracking> trackings)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                var query = trackings.GroupBy(x => x.po_no)
                    .Select(y => y.FirstOrDefault());
                foreach (var tracking in query)
                {
                    context.OrderTracking.AddOrUpdate(tracking);
                }
                context.SaveChanges();
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Configuration.ValidateOnSaveEnabled = true;
            }
        }
    }
}
