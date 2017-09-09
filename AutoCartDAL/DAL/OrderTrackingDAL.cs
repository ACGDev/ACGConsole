using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        public static Dictionary<string,order_tracking> GetOrderTracking(string connectionString)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.OrderTracking.Where(I => I.processed == 0).Join(
                    context.Orders.Where(J => J.billemail == "support@justfeedwebsites.com"),
                    tracking => tracking.po_no, order => order.orderno, (tracking, orders) => new 
                    {
                        Key = orders.po_no,
                        Value = tracking
                    }).ToDictionary(a=>a.Key,a => a.Value);
            }
        }

        public static void UpdateOrderStatus(string connectionString, List<order_tracking> trackingList)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                foreach (var tracking in trackingList)
                {
                    context.Entry(tracking).State = EntityState.Modified;
                    context.OrderTracking.Attach(entity: tracking);
                    tracking.processed = 1;
                    context.Entry(tracking).Property(I => I.processed).IsModified = true;
                }
                context.SaveChanges();
            }
        }
    }
}
