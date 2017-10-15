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
        public static void SaveJFWOrders(string connectionString, List<jfw_orders> orders, string filename)
        {

            using (var context = new AutoCareDataContext(connectionString))
            {
                foreach (var order in orders)
                {
                    order.Filename = filename;
                    context.JFWOrders.Add(order);
                }
                context.SaveChanges();
            }
        }

        public static void SaveOrderTracking(string connectionString, List<order_tracking> trackings)
        {
			DateTime thisDateMinus2 = DateTime.Now.AddDays(-2);
            using (var context = new AutoCareDataContext(connectionString))
            {
                context.Configuration.AutoDetectChangesEnabled = false;
                context.Configuration.ValidateOnSaveEnabled = false;
                var query = trackings.GroupBy(x => x.tracking_no)
                    .Select(y => y.FirstOrDefault());
                foreach (var tracking in query)
                {
                    var existingEntry =
                        context.OrderTracking.FirstOrDefault(I => I.po_no == tracking.po_no &&
                                                         I.tracking_no == tracking.tracking_no && I.SKU == tracking.SKU);
                    // Sam: changed on Sep 20 - only change New tracking entries.
                    // Sam: Changed on Sep 28 - only update if tracking data is within last 2 days
					/*if (existingEntry != null)
                    {
                        tracking.processed = existingEntry.processed;
                    }
                    context.OrderTracking.AddOrUpdate(tracking);
					*/
                    if (existingEntry == null && tracking.ship_date> thisDateMinus2)
                    {
                        context.OrderTracking.AddOrUpdate(tracking);
                    }
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
