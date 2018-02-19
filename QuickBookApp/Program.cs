using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Web;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using AutoCarOperations.DAL;
using AutoCarOperations.Model;
using System.Configuration;
using Newtonsoft.Json.Linq;
using QuickBookApp.Model;

namespace QuickBookApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("|  Sign in with AutoCareGuys  |");
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();

            Program p = new Program();
            p.doOAuth();
            Console.ReadKey();
        }

        // client configuration
        readonly string clientID = ConfigurationManager.AppSettings["clientId"];
        readonly string clientSecret = ConfigurationManager.AppSettings["clientSecret"];
        readonly string authorizationEndpoint = ConfigurationManager.AppSettings["authorizationEndPoint"];
        readonly string tokenEndpoint = ConfigurationManager.AppSettings["tokenEndpoint"];
        readonly string quickbooksURL = ConfigurationManager.AppSettings["authorizationEndPoint"];
        string access_token;

        // ref http://stackoverflow.com/a/3978040
        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(24574);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async void doOAuth()
        {
            // Generates state and PKCE values.
            string state = randomDataBase64url(32);
            string code_verifier = randomDataBase64url(32);
            //string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));
            //const string code_challenge_method = "S256";

            // Creates a redirect URI using an available port on the loopback address.
            string redirectURI = string.Format("http://{0}:{1}/", "localhost", GetRandomUnusedPort());
            output("redirect URI: " + redirectURI);

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectURI);
            output("Listening..");
            http.Start();

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format(
                "{0}?response_type=code&scope=com.intuit.quickbooks.accounting&redirect_uri={1}&client_id={2}&state={3}",
                authorizationEndpoint,
                System.Uri.EscapeDataString(redirectURI),
                clientID,
                state);

            // Opens request in the browser.
            System.Diagnostics.Process.Start(authorizationRequest);

            // Waits for the OAuth authorization response.
            var context = await http.GetContextAsync();

            // Brings the Console to Focus.
            BringConsoleToFront();

            // Sends an HTTP response to the browser.
            var response = context.Response;
            string responseString =
                string.Format(
                    "<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            System.Threading.Tasks.Task responseTask = responseOutput.WriteAsync(buffer, 0, buffer.Length).ContinueWith(
                (task) =>
                {
                    responseOutput.Close();
                    http.Stop();
                    Console.WriteLine("HTTP server stopped.");
                });

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                output(String.Format("OAuth authorization error: {0}.", context.Request.QueryString.Get("error")));
                return;
            }
            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                output("Malformed authorization response. " + context.Request.QueryString);
                return;
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incoming_state = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incoming_state != state)
            {
                output(String.Format("Received request with invalid state ({0})", incoming_state));
                return;
            }
            output("Authorization code: " + code);
            // Starts the code exchange at the Token Endpoint.
            performCodeExchange(code, code_verifier, redirectURI);
        }

        async void performCodeExchange(string code, string code_verifier, string redirectURI)
        {
            output("Exchanging code for tokens...");
            string tokenRequestBody = string.Format("grant_type=authorization_code&code={0}&redirect_uri={1}",
                code,
                System.Uri.EscapeDataString(redirectURI)
            );
            string cred = string.Format("{0}:{1}", clientID, clientSecret);
            string enc = Convert.ToBase64String(Encoding.ASCII.GetBytes(cred));
            string basicAuth = string.Format("{0} {1}", "Basic", enc);

            // sends the request
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(tokenEndpoint);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "application/json";
            //tokenRequest.Host = "oauth.platform.intuit.com";
            tokenRequest.Headers[HttpRequestHeader.Authorization] = basicAuth;
            byte[] _byteVersion = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = _byteVersion.Length;
            Stream stream = tokenRequest.GetRequestStream();
            await stream.WriteAsync(_byteVersion, 0, _byteVersion.Length);
            stream.Close();

            try
            {
                // gets the response
                HttpWebResponse tokenResponse = (HttpWebResponse)(await tokenRequest.GetResponseAsync());

                //WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    Console.WriteLine(responseText);

                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded =
                        JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

                    access_token = tokenEndpointDecoded["access_token"];
                    var configData = new ConfigurationData
                    {
                        ConnectionString = ConfigurationManager.ConnectionStrings["mysqlconnection"].ConnectionString
                    };

                    var customers = await CreateCustomers(configData);

                    var products = await CreateProducts(configData);

                    await CreateOrders(configData, customers, products);
                    Console.ReadKey();
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        output("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = await reader.ReadToEndAsync();
                            output(responseText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async Task CreateOrders(ConfigurationData configData, List<customers> customers, List<products> products)
        {
            List<string> dealers = ProductDAL.GetDistinctDealerItem(configData.ConnectionString);
            List<orders> orders = OrderDAL.FetchOrders(configData.ConnectionString,
                order => order.orderdate >= DateTime.Parse("12/11/2017"));
            foreach (var o in orders)
            {
                if (!string.IsNullOrEmpty(o.orderno))
                {
                    var customer = customers.FirstOrDefault(I => I.customer_id == o.customerid);
                    if (customer != null && customer.qb_customer_id.HasValue)
                    {
                        string response = await CreateInvoice(quickbooksURL, o, customer.qb_customer_id.Value.ToString(),
                            customer.non_taxable, products, (dealers.Contains(customer.billing_firstname)));
                        if (!response.StartsWith("error"))
                        {
                            //JToken outer = JToken.Parse(response);
                            //var pId = outer["Invoice"]["Id"].Value<int>();
                            //o.qb_product_id = pId;
                            //ProductDAL.UpdateProduct(configData, pId, prod.SKU);
                        }
                    }
                }
            }
        }

        private async Task<List<products>> CreateProducts(ConfigurationData configData)
        {
            List<products> products = ProductDAL.GetProducts(configData);
            foreach (var prod in products)
            {
                if (prod.qb_product_id != null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(prod.description))
                {
                    string response = await CreateItem(quickbooksURL, $"{prod.SKU}_{prod.description}", prod.SKU);
                    if (!response.StartsWith("error"))
                    {
                        JToken outer = JToken.Parse(response);
                        var pId = outer["Item"]["Id"].Value<int>();
                        prod.qb_product_id = pId;
                        ProductDAL.UpdateProduct(configData, pId, prod.SKU);
                    }
                    else
                    {
                        response = response.Replace("error: ", "");
                        try
                        {
                            JToken outer = JToken.Parse(response);
                            var fId = outer["Fault"]["Error"][0]["code"].Value<string>();
                            if (fId == "6240")
                            {
                                response = await RestAPI(
                                    $"query?query=select Id,Sku from item where Sku = '{prod.SKU}'", "", "GET");
                                outer = JToken.Parse(response);
                                var queryresponse = outer["QueryResponse"]["Item"];
                                foreach (var query in queryresponse)
                                {
                                    //string email = query["Sku"].Value<string>();
                                    //if (email == prod.SKU)
                                    {
                                        var pId = query["Id"].Value<int>();
                                        prod.qb_product_id = pId;
                                        ProductDAL.UpdateProduct(configData, pId, prod.SKU);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            output("Failed to update product" + prod.SKU);
                        }
                    }
                }
            }
            return products;
        }

        private async Task<List<customers>> CreateCustomers(ConfigurationData configData)
        {
            var customers = CustomerDAL.GetAll(configData);
            foreach (var customer in customers)
            {
                if (customer.qb_customer_id != null)
                {
                    continue;
                }
                string response = await CreateCustomer(quickbooksURL, customer);

                if (!response.StartsWith("error"))
                {
                    JToken outer = JToken.Parse(response);
                    var cId = outer["Customer"]["Id"].Value<int>();
                    customer.qb_customer_id = cId;
                    CustomerDAL.UpdateCustomer(configData, cId, customer.customer_id);
                }
                else
                {
                    response = response.Replace("error: ", "");
                    try
                    {
                        JToken outer = JToken.Parse(response);
                        var pId = outer["Fault"]["Error"][0]["code"].Value<string>();
                        if (pId == "6240")
                        {
                            response = await RestAPI("query?query=select Id,PrimaryEmailAddr from" +
                                          " customer where DisplayName = '" +
                                          (customer.email.Trim()) + "'", "", "GET");
                            outer = JToken.Parse(response);
                            var queryresponse = outer["QueryResponse"]["Customer"];
                            foreach (var query in queryresponse)
                            {
                                string email = query["PrimaryEmailAddr"]["Address"].Value<string>();
                                if (email == customer.email.Trim())
                                {
                                    var cId = query["Id"].Value<int>();
                                    customer.qb_customer_id = cId;
                                    CustomerDAL.UpdateCustomer(configData, cId, customer.customer_id);
                                }
                            }
                        }
                    }
                    catch
                    {
                        output("Failed To update customer" + customer.email);
                    }
                }
            }
            return customers;
        }

        /// <summary>
        /// Appends the given string to the on-screen log, and the debug console.
        /// </summary>
        /// <param name="output">string to be appended</param>
        public void output(string output)
        {
            Console.WriteLine(output);
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        public static string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        // Hack to bring the Console window to front.
        // ref: http://stackoverflow.com/a/12066376

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public void BringConsoleToFront()
        {
            SetForegroundWindow(GetConsoleWindow());
        }

        #region QB API
        public async Task<string> CreateInvoice(string url, orders order,
            string qbCustomerId, bool? nonTaxable, List<products> products, bool isDealer = false)
        {
            List<LineItem> objList = new List<LineItem>();
            foreach (var item in order.order_items)
            {
                var prod = products.FirstOrDefault(I => I.catalogid == item.catalogid);
                if (prod == null)
                {
                    return "error";
                }
                var price = item.optionprice == 0 ? item.unitprice : item.optionprice;
                objList.Add(
                    new LineItem
                    {
                        Amount = price * item.numitems,
                        DetailType = "SalesItemLineDetail",
                        SalesItemLineDetail = new SalesItemLineDetail
                        {
                            ItemRef = new ItemRef
                            {
                                value = prod.qb_product_id.ToString(),
                                name = (prod.SKU + "_" + prod.description).Length > 100 ? (prod.SKU + "_" + prod.description).Substring(0, 100) : (prod.SKU + "_" + prod.description)
                            },
                            UnitPrice = price,
                            Qty = Convert.ToInt32(item.numitems),
                            TaxCodeRef = new TaxCodeRef
                            {
                                value = nonTaxable.HasValue && nonTaxable.Value ? "NON" : "TAX"
                            }
                        }
                    }
                );
            }
            if (order.discount > 0)
            {
                objList.Add(new LineItem()
                {
                    Amount = order.orderamount * order.discount / 100,
                    DetailType = "DiscountLineDetail",
                    DiscountLineDetail = new DiscountLineDetail()
                    {
                        PercentBased = true,
                        DiscountPercent = order.discount.Value,
                        DiscountAccountRef = new ItemRef()
                        {
                            name = "Discounts",
                            value = "93"
                        }//todo: Account Ref
                    }
                });
            }
            if (order.shipcost > 0)
            {
                objList.Add(new LineItem()
                {
                    Amount = order.shipcost,
                    DetailType = "SalesItemLineDetail",
                    SalesItemLineDetail = new SalesItemLineDetail()
                    {
                        ItemRef = new ItemRef()
                        {
                            value = "128",
                            name = "Shipping"
                        }//todo: Account Ref
                    }
                });
            }
            object invoice;
            if (nonTaxable.HasValue && nonTaxable.Value)
            {
                invoice = new
                {
                    Line = objList,
                    SalesTermRef = new
                    {
                        value = order.billfirstname == "JFW" ? "9" : "6"
                    },
                    BillEmail = new
                    {
                        Address = order.billemail
                    },
                    CustomerRef = new
                    {
                        value = qbCustomerId
                    }
                };
            }
            else
            {
                invoice = new
                {
                    Line = objList,
                    SalesTermRef = new
                    {
                        value = order.billfirstname == "JFW" ? "9" : "6"
                    },
                    BillEmail = new
                    {
                        Address = order.billemail
                    },
                    TxnTaxDetail = new
                    {
                        TxnTaxCodeRef = new
                        {
                            value = "TAX"
                        },//todo: Tax Code ref
                        TotalTax = order.salestax,
                        TaxLine = new[] {
                            new {
                                Amount= order.salestax,
                                DetailType= "TaxLineDetail",
                                TaxLineDetail= new {
                                    TaxRateRef= new {
                                        value= "4"
                                    },//todo: need to create TaxAgency, TaxService
                                    PercentBased= true,
                                    TaxPercent= 8,
                                    NetAmountTaxable= order.orderamount
                                }
                            }
                        }
                    },
                    CustomerRef = new
                    {
                        value = qbCustomerId
                    }
                };
            }
            string response = await RestAPI((isDealer ? "invoice" : "salesreceipt") + "?minorversion=4", JsonConvert.SerializeObject(invoice));
            return response;
        }
        public async System.Threading.Tasks.Task<string> CreateCustomer(string url, customers cus)
        {
            var customer = new
            {
                BillAddr = new
                {
                    Line1 = cus.billing_address + cus.billing_address2,
                    City = cus.billing_city,
                    Country = cus.billing_country,
                    CountrySubDivisionCode = cus.billing_state,
                    PostalCode = cus.billing_zip
                },
                Notes = cus.comments,
                GivenName = cus.billing_firstname,
                MiddleName = cus.billing_lastname,
                FullyQualifiedName = cus.billing_firstname + " " + cus.billing_lastname,
                CompanyName = cus.billing_company,
                DisplayName = cus.email.Trim(),
                PrimaryPhone = new
                {
                    FreeFormNumber = cus.billing_phone
                },
                PrimaryEmailAddr = new
                {
                    Address = cus.email.Trim()
                }
            };
            string response = await RestAPI("customer?minorversion=4", JsonConvert.SerializeObject(customer));
            return response;
        }

        public async System.Threading.Tasks.Task<string> CreateItem(string url, string desc, string sku)
        {
            var item = new
            {
                Name = desc.Length > 100 ? desc.Substring(0, 100) : desc,
                IncomeAccountRef = new
                {
                    value = "96",
                    name = "Sales"
                },//todo: Account ref
                Sku = sku,
                ExpenseAccountRef = new
                {
                    value = "127",
                    name = "Cost of Goods Sold"
                },//todo: Account Ref
                Type = "NonInventory",
                TrackQtyOnHand = false
            };
            string response = await RestAPI("item?minorversion=4", JsonConvert.SerializeObject(item));
            return response;
        }
        private async Task<string> RestAPI(string url, string data, string method = "POST")
        {
            string responseText = null;
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create("https://sandbox-quickbooks.api.intuit.com/v3/company/193514690586999/" + url);
            tokenRequest.Method = method;
            tokenRequest.ContentType = "application/json";
            tokenRequest.Accept = "application/json";
            tokenRequest.Headers[HttpRequestHeader.Authorization] = "Bearer " + access_token;
            if (method == "POST")
            {
                using (Stream requestStream = tokenRequest.GetRequestStream())
                using (StreamWriter writer = new StreamWriter(requestStream))
                {
                    writer.Write(data);
                }
            }
            try
            {
                // gets the response
                using (HttpWebResponse tokenResponse = (HttpWebResponse)(await tokenRequest.GetResponseAsync()))
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    responseText = await reader.ReadToEndAsync();
                    Console.WriteLine(responseText);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    using (var response = ex.Response as HttpWebResponse)
                    {
                        if (response != null)
                        {
                            output("HTTP: " + response.StatusCode);
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {
                                // reads response body
                                responseText = "error: " + (await reader.ReadToEndAsync());
                                output(responseText);
                            }
                        }
                    }
                }
            }
            return responseText;
        }
        #endregion
    }
}
