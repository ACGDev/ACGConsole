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
}
