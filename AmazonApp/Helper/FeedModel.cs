using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp.Helper
{
    public class FeedModel
    {
        public string type { get; set; }
        public string sku { get; set; }
        public string asin { get; set; }
        public double price { get; set; }
        public int quantity { get; set; }
        public int fulfillmentLatency { get; set; }
    }
}
