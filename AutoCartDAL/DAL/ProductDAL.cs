using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;
using DCartRestAPIClient;
using Microsoft.JScript;
using Convert = System.Convert;
using MySql.Data.MySqlClient;

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

        public static List<FeedModel> GetASINForAmazonFeed(string connectionString, string feedType, ref int numTotalRecords) // ref long numTotalRecords
        {
            /* First check Channel_Sales_Helper_Details to see if any record is left from previous update attempt. If so, take top 100 from there.
             * If not, insert new set of values
             * */
            int numRecords = 5000;
            //if (feedType == "Product")
            //    numRecords = 5000;

            string sqlQuery01 = string.Format(@"select FeedType, SKU, ASIN,SalePrice, InventoryQty,HandlingTime from channel_sales_helper_details  
                where FeedType='{0}' and IsUpdated=false order by ASIN Limit 0,{1}", feedType, numRecords);
            string sqlQuery02 = string.Format(@"select Count(*) cnt from channel_sales_helper_details  
                where FeedType='{0}' and IsUpdated=false", feedType);
            List<FeedModel> li = null;
            using (var context = new AutoCareDataContext(connectionString))
            {
                var recTotal = context.Database.SqlQuery<int>(sqlQuery02).ToArray();
                numTotalRecords = recTotal[0];

                //  = Int32.Parse(<FeedModel>(sqlQuery02).ToString()) ;
                li = context.Database.SqlQuery<FeedModel>(sqlQuery01).ToList();
                if (li.Count > 0)
                    return li;
            }

            // Does not work because of mySQL datetime format - WHY ?
            //using (var context = new AutoCareDataContext(connectionString))
            //{
            //    var objDetail = context.Feed_helper_details.Where(d=> d.FeedType==feedType && d.IsUpdated==true);
            //    context.Feed_helper_details.RemoveRange(objDetail);
            //    context.SaveChanges();
            //}

            string strUpdateFilter = "";
            string sqlQuery = "";
            string strThisMySqlDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            if (feedType == "Product")
            {
                sqlQuery =
                    $@"insert into channel_sales_helper_details (ChannelName, ASIN, FeedType, SKU, SalePrice, HandlingTime, InventoryQty, IsUpdated, LastRequest, LastUpdate) 
                    select 'Amazon', b.asin_no,'Product',b.SKU_UPC,a.SalePrice,a.HandlingTime,a.InventoryQty,false,'{strThisMySqlDate}',null
                    from Channel_Sales_Helper a join CK_ASINS b on a.ProductPriceCat = b.ProductPriceCat
                    where a.ASIN = '' and b.ActiveInAmazon = 0 and (b.ForceBlock <> 'Y' or b.ForceBlock is null) and AddProducts = 1 ; ";
            }
            else
            {
                if (feedType == "Inventory")
                    strUpdateFilter = "a.UpdateInventoryOrHandling=1";
                else if (feedType == "Price")
                    strUpdateFilter = "a.UpdatePrice=1";

                sqlQuery =
                    $@"insert into channel_sales_helper_details (ChannelName, ASIN, FeedType, SKU, SalePrice, HandlingTime, InventoryQty, IsUpdated, LastRequest, LastUpdate) 
                    select 'Amazon', b.asin_no,'{feedType}',b.SKU_UPC,a.SalePrice, a.HandlingTime,a.InventoryQty,false,'{strThisMySqlDate}',null 
                    from Channel_Sales_Helper a join CK_ASINS b on a.ProductPriceCat = b.ProductPriceCat 
                    where a.ASIN = '' and b.ActiveInAmazon = 1 and (b.ForceBlock <> 'Y' or b.ForceBlock is null) and {strUpdateFilter}; ";

                sqlQuery +=
                    $@"insert into channel_sales_helper_details (ChannelName, ASIN, FeedType, SKU, SalePrice, HandlingTime, InventoryQty, IsUpdated, LastRequest, LastUpdate) 
                    select 'Amazon', b.asin_no,'{feedType}',b.SKU_UPC,a.SalePrice, a.HandlingTime,a.InventoryQty,false,'{strThisMySqlDate}',null 
                    from Channel_Sales_Helper a join CK_ASINS b on a.ASIN = b.asin_no
                    where a.ASIN <> ''  and b.ActiveInAmazon = 1 and (b.ForceBlock <> 'Y' or b.ForceBlock is null) and {strUpdateFilter} ";
            }

            // Delete all previous successful update records from channel_sales_helper_details
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText =
                    string.Format("delete from channel_sales_helper_details where FeedType='{0}' and IsUpdated=true", feedType);

                MySqlCommand cmd2 = conn.CreateCommand();
                cmd2.CommandText = sqlQuery;
                conn.Open();
                cmd.ExecuteNonQuery();
                cmd2.ExecuteNonQuery();
                conn.Close();
            }

            using (var context = new AutoCareDataContext(connectionString))
            {
                var recTotal = context.Database.SqlQuery<int>(sqlQuery02).ToArray();
                numTotalRecords = recTotal[0];
                li = context.Database.SqlQuery<FeedModel>(sqlQuery01).ToList();
            }
            return li;
        }
        public static void UpdateProductAfterAmazonFeed(string connStr, List<FeedModel> liFeed, string feedType, int nTotalAsins = 0)
        {
            StringBuilder sb = new StringBuilder("");
            string timeNow = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            string feedUpdateField = "";
            // Since liFeed is sorted on ASIN, we take the last one and update anything before that
            if (liFeed.Count == 0 || nTotalAsins== liFeed.Count)
            {
                // No feed data to be updated. Update Channel_Sales_Helperfor this feedtype
                if (feedType == "Product")
                    feedUpdateField = "AddProducts";
                else if (feedType == "Price")
                    feedUpdateField = "UpdatePrice";
                else 
                    feedUpdateField = "UpdateInventoryOrHandling";

                sb.AppendFormat(@"update Channel_Sales_Helper set {0}=false, LastUpdateDate='{1}' 
                    where {0}=true ;", feedUpdateField, timeNow);

                Console.WriteLine("Setting flag in master table to false for {0} - field {1} ", feedType, feedUpdateField);
            }
            else
            {
                string lastASIN = liFeed.Last().ASIN;
                string firstASIN = liFeed.First().ASIN;

                sb.AppendFormat("update channel_sales_helper_details set IsUpdated=true, LastUpdate='{1}' where ASIN <='{0}' and FeedType='{2}';", lastASIN, timeNow, feedType);

                if (feedType == "Price")
                {
                    sb.AppendFormat(@"update CK_ASINS a join channel_sales_helper_details b on a.Asin_no=b.ASIN and a.SKU_UPC = b.SKU 
                    set a.SalePrice=b.SalePrice, a.AmazonUpdateDate='{0}' 
                    where b.ASIN>= '{1}' and  b.ASIN<='{2}' ;", timeNow, firstASIN, lastASIN);
                }
                else if (feedType == "Inventory")
                {
                    sb.AppendFormat(@"update CK_ASINS a join channel_sales_helper_details b on a.Asin_no=b.ASIN and a.SKU_UPC = b.SKU 
                    set a.ActiveInAmazon=1, a.AmazonUpdateDate='{0}' 
                    where b.ASIN>= '{1}' and  b.ASIN<='{2}' and b.InventoryQty>0;", timeNow, firstASIN, lastASIN);

                    sb.AppendFormat(@"update CK_ASINS a join channel_sales_helper_details b on a.Asin_no=b.ASIN and a.SKU_UPC = b.SKU
                    set a.ActiveInAmazon=0, a.AmazonUpdateDate='{0}' 
                    where b.ASIN>= '{1}' and  b.ASIN<='{2}' and b.InventoryQty=0;", timeNow, firstASIN, lastASIN);
                }
                else
                {
                    sb.AppendFormat(@"update CK_ASINS a join channel_sales_helper_details b on a.Asin_no=b.ASIN and a.SKU_UPC = b.SKU 
                    set a.ActiveInAmazon=1, a.AmazonUpdateDate='{0}' 
                    where b.ASIN>= '{1}' and  b.ASIN<='{2}' and b.InventoryQty>0;", timeNow, firstASIN, lastASIN);
                }
                Console.WriteLine("Updating {0} local records at {1}", liFeed.Count, DateTime.Now);
            }
               
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sb.ToString();
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            Console.WriteLine("Updating Complete at  {0} ", DateTime.Now);
            //using (var context = new AutoCareDataContext(connStr))
            //{
            //    context.Database.ExecuteSqlCommand(sb.ToString());
            //    context.SaveChanges();
            //}
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

                /** Sam changed July 2018 - to get Categories - only the last level */
            string thisCategory = null;
                if (product.CategoryList.Count > 0)
                {
                    thisCategory = product.CategoryList[0].CategoryName;
                    foreach (var cat in product.CategoryList)
                    {
                        if (cat.CategoryName == "_Test_Hidden")
                        {
                            thisCategory = "_Test_Hidden";
                            break;
                        }
                            
                    }
                }
                   

                dbProducts.Add(new products
                {
                    SKU = product.SKUInfo.SKU,
                    catalogid = Convert.ToInt32(product.SKUInfo.CatalogID),
                    mfgid = product.MFGID,
                    accessgroup = product.AllowAccessCustomerGroupName,
                    //accessories =  null,
                    listing_displaytype = null,
                    //backorder_message = product.BackOrderMessage,
                    categoriesaaa = thisCategory,
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

       
        public static CKASINS FindOrderFromASIN(string connectionString, string ASIN, string manuf_sku)
        {
            
            using (var context = new AutoCareDataContext(connectionString))
            {
                bool bTryMFGId = false;
                var response = context.Database.SqlQuery<CKASINS>($"SELECT CA.ItemId,CA.VariantId,P.catalogid,CA.ResourceCode FROM CK_ASINS CA LEFT JOIN `3dc_products` P ON P.SKU = CA.SKU_UPC and (CA.ForceBlock<>'Y' or CA.ForceBlock is null) and not (P.categoriesaaa is null or P.categoriesaaa='_Test_Hidden') WHERE asin_no='{ASIN}'").ToList();
                if (response.Count == 0) bTryMFGId = true;
                else if (response[0].ItemId == null || response[0].catalogid == null)
                    bTryMFGId = true;
            
                if (bTryMFGId)
                {
                    // Sam: Try to get item from manuf_sku if SKU does not map
                    response = context.Database.SqlQuery<CKASINS>($"SELECT CA.ItemId,CA.VariantId,P.catalogid,CA.ResourceCode FROM CK_ASINS CA LEFT JOIN `3dc_products` P ON P.mfgid = CA.ItemID and (CA.ForceBlock<>'Y' or CA.ForceBlock is null) and not (P.categoriesaaa is null or P.categoriesaaa='_Test_Hidden')  WHERE asin_no='{ASIN}'").ToList();
                    if (response.Count == 0)
                        return null;
                }
                // var dealerPrice = context.Products.Join((context.CKVaraints.Where(I => I.SKU == sku && I.Blocked.ToLower() == "no").Join(context.ACGDealerItemPrice, C=> C.ItemID, A=> A.ItemID, (variant, price) => price)), i => i.mfgid, j => j.ItemID, (i, j) => j).FirstOrDefault();
                return response[0]; // 
            }
        }
    }
    public class CKASINS
    {
        public string ItemId { get; set; }
        public string VariantId { get; set; }
        public int? catalogid { get; set; }
        public string ResourceCode { get; set; }

    }
}
