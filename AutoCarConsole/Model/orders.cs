using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoCarConsole.Model
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
        public string other2 { get; set; }
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
    }

    public class products
    {
        [Key]
        public int catalogid { get; set; }
        public string SKU { get; set; }
        public string mfgid { get; set; }
        public int listing_displaytype { get; set; }
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
}
