using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoCarOperations.Model
{
    public class orders
    {
        [Key]
        public long order_id { get; set; }
        public long? customerid { get; set; }
        public DateTime? orderdate { get; set; }
        public double? orderamount { get; set; }
        public string billfirstname { get; set; }
        public string billlastname { get; set; }
        public string billemail { get; set; }
        public string billaddress { get; set; }
        public string billaddress2 { get; set; }
        public string billcity { get; set; }
        public string billzip { get; set; }
        public string billstate { get; set; }
        public string billcountry { get; set; }
        public string billphone { get; set; }
        public string ocompany { get; set; }
        public string cus_comment { get; set; }
        public double? salestax { get; set; }
        public string internalcomment { get; set; }
        public string shipcomplete { get; set; }
        public double? shipcost { get; set; }
        public string shipfirstname { get; set; }
        public string shiplastname { get; set; }
        public string shipcompany { get; set; }
        public string shipemail { get; set; }
        public string shipaddress { get; set; }
        public string shipaddress2 { get; set; }
        public string shipcity { get; set; }
        public string shipzip { get; set; }
        public string shipstate { get; set; }
        public string shipcountry { get; set; }
        public string shipphone { get; set; }
        public string paymethod { get; set; }
        public string paymethodinfo { get; set; }
        public string po_no { get; set; }
        public string ccauthorization { get; set; }
        public string errors { get; set; }
        public double? discount { get; set; }
        public int status { get; set; }
        public double? ohandling { get; set; }
        public string coupon { get; set; }
        public decimal? coupondiscount { get; set; }
        public double? coupondiscountdual { get; set; }
        public string giftcertificate { get; set; }
        public decimal? giftamountused { get; set; }
        public double? giftamountuseddual { get; set; }
        public string orderno { get; set; }
        public string invoicenum_prefix { get; set; }
        public int? invoicenum { get; set; }
        public int? order_status { get; set; }
        public string referer { get; set; }
        public string salesperson { get; set; }
        public double? affiliate_approved { get; set; }
        public string affiliate_approvedreason { get; set; }
        public int? affiliate_id { get; set; }
        public DateTime? date_started { get; set; }
        public string ip { get; set; }
        public int? last_auto_email { get; set; }
        public DateTime? last_update { get; set; }
        public int? orderboxes { get; set; }
        public string orderkey { get; set; }
        public string ostep { get; set; }
        public double? ordertax2 { get; set; }
        public double? ordertax3 { get; set; }
        public double? orderweight { get; set; }
        public int? shipmethodid { get; set; }
        public string userid { get; set; }
        public double? affiliate_commission { get; set; }
        public int? isrecurrent { get; set; }
        public int? recurrent_frequency { get; set; }
        public long? parent_orderid { get; set; }
        public DateTime? last_order { get; set; }
        public DateTime? next_order { get; set; }
        [ForeignKey("order_id")]
        public List<order_items> order_items { get; set; }
        [ForeignKey("order_id")]
        public List<order_shipments> order_shipments { get; set; }
        [NotMapped]
        public List<order_item_details> order_item_details { get; set; }
    }

    public class order_items
    {
        [Key]
        public int order_item_id { get; set; }
        [ForeignKey("catalogid")]
        public products Product { get; set; }
        public long? order_id { get; set; }
        public int? catalogid { get; set; }
        public double? numitems { get; set; }
        public string itemname { get; set; }
        public double? unitprice { get; set; }
        public int? supplierid { get; set; }
        public double? weight { get; set; }
        public double? optionprice { get; set; }
        public string additional_field1 { get; set; }
        public string additional_field2 { get; set; }
        public string additional_field3 { get; set; }
        public string itemid { get; set; }
        public string options { get; set; }
        public string catalogidoptions { get; set; }
        public int? shipment_id { get; set; }
        public DateTime? date_added { get; set; }
        public string itemdescription { get; set; }
        public double? unitcost { get; set; }
        public double? unitstock { get; set; }
        public int? recurrent { get; set; }
        public int? depends_on_item { get; set; }
        public int? recurring_order_frequency { get; set; }
        public string variant_id { get; set; }
    }

    public class products
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int catalogid { get; set; }

        public string SKU { get; set; }
        public string name { get; set; }
        public string categoriesaaa { get; set; }
        public string mfgid { get; set; }
        public string manufacturer { get; set; }
        //public string distributor { get; set; }
        public double? cost { get; set; }
        public double? price { get; set; }
        public double? price2 { get; set; }
        public double? price3 { get; set; }
        public double? saleprice { get; set; }
        public int? onsale { get; set; }
        public int? stock { get; set; }
        public string stock_alert { get; set; }
        //public string display_stock { get; set; }
        public int? weight { get; set; }
        public string minimumorder { get; set; }
        public string maximumorder { get; set; }
        public string date_created { get; set; }
        public string description { get; set; }
        public string extended_description { get; set; }
        public string keywords { get; set; }
        public int? sorting { get; set; }
        public string thumbnail { get; set; }
        public string image1 { get; set; }
        public string image2 { get; set; }
        public string image3 { get; set; }
        public string image4 { get; set; }
        //public string realmedia { get; set; }
        //public string related { get; set; }
        //public string accessories { get; set; }
        public double? shipcost { get; set; }
        public string imagecaption1 { get; set; }
        public string imagecaption2 { get; set; }
        public string imagecaption3 { get; set; }
        public string imagecaption4 { get; set; }
        public string title { get; set; }
        public string metatags { get; set; }
        public string displaytext { get; set; }
        //public string eproduct_password { get; set; }
        //public string eproduct_random { get; set; }
        //public string eproduct_expire { get; set; }
        //public string eproduct_path { get; set; }
        //public string eproduct_serial { get; set; }
        //public string eproduct_instructions { get; set; }
        public bool? homespecial { get; set; }
        public bool? categoryspecial { get; set; }
        public bool? hide { get; set; }
        public bool? free_shipping { get; set; }
        public bool? nontax { get; set; }
        public bool? notforsale { get; set; }
        //public bool? giftcertificate { get; set; }
        public string userid { get; set; }
        public string last_update { get; set; }
        public string extra_field_1 { get; set; }
        public string extra_field_2 { get; set; }
        public string extra_field_3 { get; set; }
        public string extra_field_4 { get; set; }
        public string extra_field_5 { get; set; }
        public string extra_field_6 { get; set; }
        public string extra_field_7 { get; set; }
        public string extra_field_8 { get; set; }
        public string extra_field_9 { get; set; }
        public string extra_field_10 { get; set; }
        public string extra_field_11 { get; set; }
        public string extra_field_12 { get; set; }
        public string extra_field_13 { get; set; }
        //public int? usecatoptions { get; set; }
        //public string qtyoptions { get; set; }
        public double? price_1 { get; set; }
        public double? price_2 { get; set; }
        public double? price_3 { get; set; }
        public double? price_4 { get; set; }
        public double? price_5 { get; set; }
        public double? price_6 { get; set; }
        public double? price_7 { get; set; }
        public double? price_8 { get; set; }
        public double? price_9 { get; set; }
        public double? price_10 { get; set; }
        public bool? hide_1 { get; set; }
        public bool? hide_2 { get; set; }
        public bool? hide_3 { get; set; }
        public bool? hide_4 { get; set; }
        public bool? hide_5 { get; set; }
        public bool? hide_6 { get; set; }
        public bool? hide_7 { get; set; }
        public bool? hide_8 { get; set; }
        public bool? hide_9 { get; set; }
        public bool? hide_10 { get; set; }
        //public int? minorderpkg { get; set; }
        public int? listing_displaytype { get; set; }
        //public int? show_out_stock { get; set; }
        public bool? pricing_groupopt { get; set; }
        //public bool? qtydiscount_opt { get; set; }
        public int? loginlevel { get; set; }
        //public string redirectto { get; set; }
        public string accessgroup { get; set; }
        //public int? self_ship { get; set; }
        //public string tax_code { get; set; }
        //public string eproduct_reuseserial { get; set; }
        public bool? nonsearchable { get; set; }
        //public string instock_message { get; set; }
        //public string outofstock_message { get; set; }
        //public string backorder_message { get; set; }
        public int? height { get; set; }
        public int? width { get; set; }
        public double? depth { get; set; }
        //public int? reward_points { get; set; }
//        public int? reward_disable { get; set; }
        //public int? reward_redeem { get; set; }
        public string filename { get; set; }
        //public string fractional_qty { get; set; }
        public string gtin { get; set; }
        //public string rma_maxperiod { get; set; }
        //public string recurring_order { get; set; }
        //public string reminders_enabled { get; set; }
        //public string reminders_frequency { get; set; }
        //public string review_average { get; set; }
        //public int? review_count { get; set; }
    }

    public class order_shipments
    {
        [Key]
        public int order_shipment_id{get;set;}
        public DateTime? last_update {get;set;}
        public int? order_status {get;set;}
        public long? order_id {get;set;}
        public int? catalogid {get;set;}
        public string oshipmethod {get;set;}
        public int? oshipmethodid {get;set;}
        public string oshippeddate {get;set;}
        public string trackingcode {get;set;}
        public int? shipping_id { get; set; }
    }

    public class order_tracking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string po_no { get; set; }
        public long order_no { get; set; }
        public DateTime order_date { get; set; }
        public string SKU { get; set; }
        public string ship_address { get; set; }
        public DateTime ship_date { get; set; }
        public string tracking_no { get; set; }
        public string ship_agent { get; set; }
        public string ship_service { get; set; }

        public int processed { get; set; }
    }

    public class jfw_orders
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long jfw_order_id { get; set; }
        public string PO { get; set; }
        public DateTime PO_Date { get; set; }
        public string Ship_Company { get; set; }
        public string Ship_Name { get; set; }
        public string Ship_Addr { get; set; }
        public string Ship_Addr_2 { get; set; }
        public string Ship_City { get; set; }
        public string Ship_State { get; set; }
        public string Ship_Zip { get; set; }
        public string Ship_Country { get; set; }
        public string Ship_Phone { get; set; }
        public string Ship_Email { get; set; }
        public string Ship_Service { get; set; }
        public string CK_SKU { get; set; }
        public string CK_Item { get; set; }
        public string CK_Variant { get; set; }
        public string Customized_Code { get; set; }
        public string Customized_Msg { get; set; }
        public string Customized_Code2 { get; set; }
        public int? Qty { get; set; }
        public string Comment { get; set; }
        public string Customized_Msg2 { get; set; }
        public string Filename { get; set; }
    }

    public class order_item_details
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long order_item_details_id { get; set; }
        public int order_item_id { get; set; }
        public string order_no { get; set; }
        public int? sequence_no { get; set; }
        public string mfg_item_id { get; set; }
        public string sku { get; set; }
        public string production_slno { get; set; }
        public string status { get; set; }
        public DateTime? status_datetime { get; set; }
        public string ship_agent { get; set; }
        public string ship_service_code { get; set; }
        public string tracking_no { get; set; }
        public DateTime? ship_date { get; set; }
        public bool? returned { get; set; }
        public DateTime? Return_date { get; set; }
        public DateTime? acg_inv_date { get; set; }
        public DateTime? ACG_inv_pay_date { get; set; }
        public string tracking_link { get; set; }
        public string description { get; set; }
    }

    public class ordertemplate
    {
        public string Name { get; set; }
        public string OrderNo { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public List<object> OrderList { get; set; }
    }
}