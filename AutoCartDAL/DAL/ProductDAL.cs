using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;
using DCartRestAPIClient;
using Microsoft.JScript;
using Convert = System.Convert;

namespace AutoCarOperations.DAL
{
    public static class ProductDAL
    {
        public static List<products> GetProducts(ConfigurationData config, Func<products, bool> condFunc = null)
        {
            using (var context = new AutoCareDataContext(config.ConnectionString))
            {
                if (condFunc != null)
                {
                    return context.Products.Where(condFunc).ToList();
                }
                return context.Products.ToList();
            }
        }
        public static void UpdateProduct(ConfigurationData config, int qbId, string sku)
        {
            using (var context = new AutoCareDataContext(config.ConnectionString))
            {
                context.Database.ExecuteSqlCommand(
                    $"UPDATE `3dc_products` SET qb_product_id = {qbId} WHERE SKU = '{sku}'");
                context.SaveChanges();
            }
        }
        public static void AddProduct(ConfigurationData config)
        {
            Console.WriteLine("..........Fetch Product Record..........");

            List<Product> products = new List<Product>();
            var skip = 0;

            while (true)
            {
                var records = RestHelper.GetRestAPIRecords<Product>("", "Products", config.PrivateKey, config.Token, config.Store, "100", skip, "");
                int counter = records.Count;
                Console.WriteLine("..........Fetches " + counter + " Product Record..........");
                products.AddRange(records);
                AddProducts(config.ConnectionString, products);
                if (counter < 100)
                {
                    break;
                }
                skip = 101 + skip;
                products = new List<Product>();
            }
            Console.WriteLine("..........Finished..........");
        }
        private static void AddProducts(string connectionString, List<Product> products)
        {
            List<products> productDB = GetProducts(products);
            using (var context = new AutoCareDataContext(connectionString))
            {
                foreach (var prod in productDB)
                {
                    context.Products.AddOrUpdate(prod);
                }
                context.SaveChanges();
            }
        }
        /// <summary>
        /// Convert REST Product List to DB products
        /// </summary>
        /// <param name="products"></param>
        /// <returns></returns>
        private static List<products> GetProducts(List<Product> products)
        {
            List<products> dbProducts = new List<products>();

            foreach (var product in products)
            {
                DateTime upd_dt;
                if (!DateTime.TryParse(product.LastUpdate.ToString(), out upd_dt))
                    upd_dt = DateTime.Now;

                dbProducts.Add(new products
                {
                    SKU = product.SKUInfo.SKU,
                    catalogid = Convert.ToInt32(product.SKUInfo.CatalogID),
                    mfgid = product.MFGID,
                    accessgroup = product.AllowAccessCustomerGroupName,
                    //accessories =  null,
                    listing_displaytype = null,
                    //backorder_message = product.BackOrderMessage,
                    categoriesaaa = null,
                    categoryspecial = product.CategorySpecial,//changed int to boolean
                    cost = product.SKUInfo.Cost,
                    date_created = Convert.ToDateTime(product.DateCreated).ToString("yyyy-MM-ddTHH:mm:ss"),
                    depth = product.Depth,//changed int to double
                    description = product.ShortDescription,
                    displaytext = product.DisplayText,
                    //display_stock = product.OutOfStockMessage,
                    //distributor = product.DistributorList != null && product.DistributorList.Count > 0 ? product.DistributorList[0].DistributorName : string.Empty,
                    //eproduct_expire = null,
                    //eproduct_instructions = product.SpecialInstructions,
                    //eproduct_password = null,
                    //eproduct_path = null,
                    //eproduct_random = null,
                    //eproduct_reuseserial = product.SerialList != null && product.SerialList.Count > 0? product.SerialList[0].SerialUses.ToString(): string.Empty,
                    //eproduct_serial = product.SerialList != null && product.SerialList.Count > 0 ? product.SerialList[0].SerialCode : string.Empty,
                    extended_description = product.Description,
                    extra_field_1 = product.ExtraField1,
                    extra_field_10 = product.ExtraField10,
                    extra_field_11 = product.ExtraField11,
                    extra_field_12 = product.ExtraField12,
                    extra_field_13 = product.ExtraField13,
                    extra_field_2 = product.ExtraField2,
                    extra_field_3 = product.ExtraField3,
                    extra_field_4 = product.ExtraField4,
                    extra_field_5 = product.ExtraField5,
                    extra_field_6 = product.ExtraField6,
                    extra_field_7 = product.ExtraField7,
                    extra_field_8 = product.ExtraField8,
                    extra_field_9 = product.ExtraField9,
                    filename = product.CustomFileName,
                    //fractional_qty = null,//product.AllowFractionalQuantity
                    free_shipping = product.FreeShipping,//change to bit
                    //giftcertificate = product.GiftCertificate,//changed to bit
                    gtin = product.GTIN,
                    height = Convert.ToInt32(product.Height),//not changed
                    hide = product.Hide,//changed to bit
                    hide_1=product.PriceLevel1Hide,
                    hide_10 = product.PriceLevel10Hide,
                    hide_2 = product.PriceLevel2Hide,
                    hide_3 = product.PriceLevel3Hide,
                    hide_4 = product.PriceLevel4Hide,
                    hide_5 = product.PriceLevel5Hide,
                    hide_6 = product.PriceLevel6Hide,
                    hide_7 = product.PriceLevel7Hide,
                    hide_8 = product.PriceLevel8Hide,
                    hide_9 = product.PriceLevel9Hide,
                    homespecial = product.HomeSpecial,
                    //change all
                    image1 = product.MainImageFile,
                    image2 = product.AdditionalImageFile2,
                    image3 = product.AdditionalImageFile3,
                    image4 = product.AdditionalImageFile4,
                    imagecaption1 = product.MainImageCaption,
                    imagecaption2 = product.AdditionalImageCaption2,
                    imagecaption3 = product.AdditionalImageCaption3,
                    imagecaption4 = product.AdditionalImageCaption4,
                    //instock_message = product.InStockMessage,
                    keywords = product.Keywords,
                    last_update = upd_dt.ToString("yyyy-MM-ddTHH:mm:ss"),  // product.LastUpdate.ToString(),
                    loginlevel = product.LoginRequiredOptionID,
                    manufacturer = product.ManufacturerID.ToString(),
                    maximumorder = product.MaximumQuantity.ToString(),
                    metatags = product.MetaTags,
                    minimumorder = product.MinimumQuantity.ToString(),
                    //minorderpkg = null,
                    name = null,
                    nonsearchable = product.NonSearchable,//changed to bit
                    nontax = product.NonTaxable,//changed to bit
                    notforsale = product.NotForSale,//changed to bit
                    onsale = product.StockAlert,
                    //outofstock_message = product.OutOfStockMessage,
                    price = product.SKUInfo.Price,
                    price2 = product.PriceLevel2,
                    price3 = product.PriceLevel3,
                    price_1 = product.PriceLevel1,
                    price_10 = product.PriceLevel10,
                    price_2 = product.PriceLevel2,
                    price_3 = product.PriceLevel3,
                    price_4 = product.PriceLevel4,
                    price_5 = product.PriceLevel5,
                    price_6 = product.PriceLevel6,
                    price_7 = product.PriceLevel7,
                    price_8 = product.PriceLevel8,
                    price_9 = product.PriceLevel9,
                    pricing_groupopt = product.GroupOptionsForQuantityPricing,
                    //qtydiscount_opt = product.GroupOptionsForQuantityPricing,
                    //qtyoptions = product.QuantityOptions,
                    //realmedia = product.MediaFile,
                    //recurring_order = null,
                    //redirectto = product.LoginRequiredOptionRedirectTo,
                    //related = product.RelatedProductList != null && product.RelatedProductList.Count > 0 ? product.RelatedProductList[0].RelatedProductID.ToString() : null,
                    //reminders_enabled = null,
                    //reminders_frequency = null,
                    //review_average = null,
                    //review_count = null,
                    //reward_disable = product.DisableRewards.HasValue && product.DisableRewards.Value ? 1: 0,
                    //reward_points = product.RewardPoints,
                    //reward_redeem = product.RedeemPoints,
                    //rma_maxperiod = product.RMAMaxPeriod,
                    saleprice = null,
                    //self_ship = product.SelfShip.HasValue && product.SelfShip.Value ? 1: 0,
                    shipcost = product.ShipCost,
                    //show_out_stock = null,
                    sorting = null,
                    stock = product.StockAlert,
                    stock_alert = product.StockAlert.ToString(),
                    //tax_code = product.TaxCode,
                    thumbnail = product.ThumbnailFile,
                    title = product.Title,
                    //usecatoptions = null,
                    userid = product.UserID,
                    weight = Convert.ToInt32(product.Weight),
                    width = Convert.ToInt32(product.Width),
                });
            }
            return dbProducts;
        }

        public static List<string> GetDistinctDealerItem(string connectionString)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.ACGDealerItemPrice.Select(I => I.DealerCode).Distinct().ToList();
            }
        }
        public static Tuple<products,DealerItemPrice> FindOrderFromSKU(string connectionString, string sku)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                var product = context.Products.Join(context.CKVaraints.Where(I => I.SKU == sku && I.Blocked.ToLower() == "no"), i => i.mfgid, j => j.ItemID, (i, j) => i).FirstOrDefault();
                // var dealerPrice = context.Products.Join((context.CKVaraints.Where(I => I.SKU == sku && I.Blocked.ToLower() == "no").Join(context.ACGDealerItemPrice, C=> C.ItemID, A=> A.ItemID, (variant, price) => price)), i => i.mfgid, j => j.ItemID, (i, j) => j).FirstOrDefault();
                DealerItemPrice dealerPrice = null;
                if ( null != product)
                    dealerPrice = context.ACGDealerItemPrice.FirstOrDefault(I => I.ItemID == product.SKU);   // Sam 01/05/18

                return Tuple.Create(product, dealerPrice);
            }
        } 
    }
}
