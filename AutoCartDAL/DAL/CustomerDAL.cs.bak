using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;
using DCartRestAPIClient;

namespace AutoCarOperations.DAL
{
    public static class CustomerDAL
    {
        public static List<customers> GetAll(ConfigurationData config, Func<customers, bool> condFunc = null)
        {
            using (var context = new AutoCareDataContext(config.ConnectionString))
            {
                if (condFunc == null)
                {
                    return context.Customers.OrderBy(I => I.customer_id).ToList();
                }
                return context.Customers.Where(condFunc).OrderBy(I => I.customer_id).ToList();
            }
        }

        public static customers FindCustomer(string connectionString, Func<customers,bool> condFunc)
        {
            using (var context = new AutoCareDataContext(connectionString))
            {
                return context.Customers.FirstOrDefault(condFunc);
            }
        }
        public static void UpdateCustomer(ConfigurationData config, int qbId, long customerId)
        {
            using (var context = new AutoCareDataContext(config.ConnectionString))
            {
                context.Database.ExecuteSqlCommand(
                    $"UPDATE customers SET qb_customer_id = {qbId} WHERE customer_id = {customerId}");
                context.SaveChanges();
            }
        }
        public static void AddCustomer(ConfigurationData config)
        {
            Console.WriteLine("..........Fetch Customer Record..........");

            var customerGroups = RestHelper.GetRestAPIRecords<CustomerGroup>("", "CustomerGroups", config.PrivateKey, config.Token, config.Store, "100", 0, "");
            List<Customer> customers = new List<Customer>();
            var skip = 0;

            while (true)
            {
                var records = RestHelper.GetRestAPIRecords<Customer>("", "Customers", config.PrivateKey, config.Token, config.Store, "200", skip, "");
                int counter = records.Count;
                Console.WriteLine("..........Fetches " + counter + " Customer Record..........");
                customers.AddRange(records);
                AddCustomers(config.ConnectionString, customers, customerGroups);
                if (counter < 200)
                {
                    break;
                }
                skip = 201 + skip;
            }
            Console.WriteLine("..........Finished..........");
        }
        private static void AddCustomers(string connectionString, List<Customer> customers, List<CustomerGroup> customerGroups)
        {
            var customerAndGroup = GetCustomers(customers, customerGroups);
            var customerDB = customerAndGroup.Item1;
            var customerGroupDB = customerAndGroup.Item2;            
            using (var context = new AutoCareDataContext(connectionString))
            {
                foreach (var cust in customerDB)
                {
                    context.Customers.AddOrUpdate(cust);
                }
                foreach (var custGrp in customerGroupDB)
                {
                    context.CustomerGroups.AddOrUpdate(custGrp);
                }
                context.SaveChanges();                    
            }
        }
        /// <summary>
        /// Convert REST Customer List to DB customers
        /// </summary>
        /// <param name="customers"></param>
        /// <param name="customerGroups"></param>
        /// <returns></returns>
        private static Tuple<List<customers>, List<customer_groups>> GetCustomers(List<Customer> customers, List<CustomerGroup> customerGroups)
        {
            List<customers> dbCustomers = new List<customers>();
            List<customer_groups> dbCustomerGroups = new List<customer_groups>();
            
            foreach (var group in customerGroups)
            {
                dbCustomerGroups.Add(new customer_groups
                {
                    allow_registration = group.AllowRegistration,
                    auto_approve = group.AutoApprove,
                    customer_group_desc = group.Description,
                    customer_group_id = group.CustomerGroupID,
                    customer_group_name = group.Name,
                    disable_reward_points = group.DisableRewardPoints,
                    minimum_order = group.MinimumOrder,
                    non_taxable = group.NonTaxable,
                    price_level = group.PriceLevel,
                    registration_message = group.RegistrationMessage
                });
            }
            foreach (var customer in customers)
            {
                var group = customerGroups.FirstOrDefault(I => I.CustomerGroupID == customer.CustomerGroupID);
                dbCustomers.Add(new customers
                {
                    billing_address = customer.BillingAddress1,
                    billing_address2 = customer.BillingAddress2,
                    billing_city = customer.BillingCity,
                    billing_company = customer.BillingCompany,
                    billing_country = customer.BillingCountry,
                    billing_firstname = customer.BillingFirstName,
                    billing_lastname = customer.BillingLastName,
                    billing_phone = customer.BillingPhoneNumber,
                    billing_state = customer.BillingState,
                    billing_zip = customer.BillingZipCode,
                    customer_id = customer.CustomerID,
                    password = customer.Password,
                    active = customer.Enabled,
                    comments = customer.Comments,
                    email = customer.Email,
                    maillist = customer.MailList,
                    non_taxable= customer.NonTaxable,
                    pricelevel = group == null ? (int?)null : group.PriceLevel,
                    shipping_address = customer.ShippingAddress1,
                    shipping_address2 = customer.ShippingAddress2,
                    shipping_city = customer.ShippingCity,
                    shipping_company = customer.ShippingCompany,
                    shipping_country = customer.ShippingCountry,
                    shipping_firstname = customer.ShippingFirstName,
                    shipping_lastname = customer.ShippingLastName,
                    shipping_phone = customer.ShippingPhoneNumber,
                    shipping_state = customer.ShippingState,
                    shipping_zip = customer.ShippingZipCode,
                    additional_field3 = customer.AdditionalField3,
                    additional_field2 = customer.AdditionalField2,
                    additional_field1 = customer.AdditionalField1,
                    additional_field4 = "Fake",
                    website = "www.autocareguys.com",
                    accountno = "Fake",
                    discount = 0,
                    customertype = 0,
                    last_update = DateTime.Now,
                    lastlogindate = DateTime.Now
                });
            }
            return new Tuple<List<customers>,List<customer_groups>>(dbCustomers, dbCustomerGroups);
        }
    }
}
