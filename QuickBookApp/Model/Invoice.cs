using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickBookApp.Model
{
    class LineItem
    {
        public double? Amount { get; set; }
        public string DetailType { get; set; }
        public SalesItemLineDetail SalesItemLineDetail { get; set; }
        public DiscountLineDetail DiscountLineDetail { get; set; }
    }

    class DiscountLineDetail
    {
        public bool PercentBased { get; set; }
        public double DiscountPercent { get; set; }
        public ItemRef DiscountAccountRef { get; set; }
    }
    class SalesItemLineDetail
    {
        public ItemRef ItemRef { get; set; }
        public TaxCodeRef TaxCodeRef { get; set; }
        public double? UnitPrice { get; set; }
        public int Qty { get; set; }
    }

    class TaxCodeRef
    {
        public string value { get; set; }
    }
    class ItemRef
    {
        public string value { get; set; }
        public string name { get; set; }
    }
}
