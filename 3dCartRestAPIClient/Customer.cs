using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace DCartRestAPIClient
{

  
    //public class Customer : RestAPIObject
    public class Customer : IRestAPIType
    {
        // ID
        public long CustomerID { get; set; }

        // Login Information

        public string Email { get; set; }

        public string Password { get; set; }

        // Billing Information

        public string BillingCompany { get; set; }

        public string BillingFirstName { get; set; }

        public string BillingLastName { get; set; }

        public string BillingAddress1 { get; set; }

        public string BillingAddress2 { get; set; }

        public string BillingCity { get; set; }

        public string BillingState { get; set; }

        public string BillingZipCode { get; set; }

        public string BillingCountry { get; set; }

        public string BillingPhoneNumber { get; set; }

        public static RestAPIType key
        {
            get
            {
                return RestAPIType.Customer;
            }
        }

        public static string Type
        {
            get
            {
                return "Customers";
            }
        }
        public string ShippingCompany { get; set; }
        public string ShippingFirstName { get; set; }
        public string ShippingLastName { get; set; }
        public string ShippingAddress1 { get; set; }
        public string ShippingAddress2 { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingZipCode { get; set; }
        public string ShippingCountry { get; set; }
        public string ShippingPhoneNumber { get; set; }
        public int ShippingAddressType { get; set; }
        public int CustomerGroupID { get; set; }
        public bool Enabled { get; set; }
        public bool MailList { get; set; }
        public bool NonTaxable { get; set; }
        public bool DisableBillingSameAsShipping { get; set; }
        public string Comments { get; set; }
        public string AdditionalField1 { get; set; }
        public string AdditionalField2 { get; set; }
        public string AdditionalField3 { get; set; }
        public string TotalStoreCredit { get; set; }
    }

  






}