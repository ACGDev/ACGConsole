using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCarOperations.Model
{
    public class customers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long customer_id{get;set;}
        public string billing_firstname{get;set;}
        public string billing_lastname{get;set;}
        public string billing_address{get;set;}
        public string billing_address2{get;set;}
        public string billing_city{get;set;}
        public string billing_state{get;set;}
        public string billing_zip{get;set;}
        public string billing_country{get;set;}
        public string billing_company{get;set;}
        public string billing_phone{get;set;}
        public string email{get;set;}
        public string shipping_firstname{get;set;}
        public string shipping_lastname{get;set;}
        public string shipping_address{get;set;}
        public string shipping_address2{get;set;}
        public string shipping_city{get;set;}
        public string shipping_state{get;set;}
        public string shipping_zip{get;set;}
        public string shipping_country{get;set;}
        public string shipping_company{get;set;}
        public string shipping_phone{get;set;}
        public string comments{get;set;}
        public DateTime lastlogindate{get;set;}
        public string website{get;set;}
        public string password{get;set;}
        public double discount{get;set;}
        public string accountno{get;set;}
        public bool maillist{get;set;}
        public bool? non_taxable { get; set; }
        public int customertype{get;set;}
        public DateTime last_update{get;set;}
        public bool active{get;set;}
        public string additional_field1{get;set;}
        public string additional_field2{get;set;}
        public string additional_field3{get;set;}
        public string additional_field4{get;set;}
        public int? pricelevel {get;set;}
        public int? qb_customer_id { get; set; }
    }

    public class customer_groups
    {
        [Key]
        public long customer_group_id { get; set; }

        public string customer_group_name { get; set; }

        public string customer_group_desc { get; set; }

        public decimal minimum_order { get; set; }

        public bool non_taxable { get; set; }

        public bool allow_registration { get; set; }

        public bool disable_reward_points { get; set; }

        public bool auto_approve { get; set; }

        public string registration_message { get; set; }

        public int price_level { get; set; }
    }
}