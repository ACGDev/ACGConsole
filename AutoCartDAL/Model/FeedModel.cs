using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCarOperations.Model
{
    public class FeedModel
    {
        public string type { get; set; }
        public string sku { get; set; }
        public string ASIN { get; set; }
        public double SalePrice { get; set; }
        public int InventoryQty { get; set; }
        public int HandlingTime { get; set; }
    }

    public class channel_sales_helper_details
    {
        [Key]
        [Column(Order = 1)]
        public string ChannelName { get; set; }
        [Key]
        [Column(Order = 2)]
        public string ASIN { get; set; }
        [Key]
        [Column(Order = 3)]
        public string FeedType { get; set; }
        public string SKU { get; set; }
        public double SalePrice { get; set; }
        public int InventoryQty { get; set; }
        public int HandlingTime { get; set; }
        public bool IsUpdated { get; set; }
        public DateTime LastRequest { get; set; }
        public DateTime? LastUpdate { get; set; }
    }
}
