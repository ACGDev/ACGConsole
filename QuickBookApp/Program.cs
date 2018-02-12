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

namespace QuickBookApp
{
    public static class QuickbooksConstants
    {
        public const string consumerKey = "Q0PVpJN0uoUBQfkfddCcYcchTfGYKDJZ1ecgKTT7yzjJOdZa6X";
        public const string consumerSecret = "JeDxiv5fKKmzTZoEmMECui8DUXztWGOKphj114Jh";

        public const string accessToken =
                "eyJlbmMiOiJBMTI4Q0JDLUhTMjU2IiwiYWxnIjoiZGlyIn0..9MkBlWoOA1Hmn5t--6UJSQ.EDpKpCM-3Vi0PASua9KV95J-H-mxkijrBzJXo7ET5YNGoCFUvqeTSOJkPu9DPo_1jx3TuKZP78e4JZr_yjkINJRQANOrNbBpPMPW_me6-_vsTQY4cEqL33OILzwIeiQg4LYmw5mFLDD2h8w19as7u-BrOLCfW_guOK0mD_9qoiLgwp-XE3NDtosbCjRk9TLK8KRN-w7rCtVbPk5F5jmhhJ5LuQbsTRGYj_faoHBIU203ctZrv-2iklCMMCQIo-dG3iN3eyBeCH6iwEa-N_UzHFF8UKnygYVRzGYnWM78J1hJn3veEFjzU2RYPyyePrLQ3Dfd_tjPFhjN54X30GbmdcncyRnb88zuE5WBqVZtX6nM7yT1pEdWhuFGipJRNGJGSWQA2QTGHPy6hG4mlr-Jkiigb_tHPkmRToQCZVNZkABu1NUQ1ksbd8dsdasdA1l7DsJSAsjbNmdwsvstF6IAffLZj7H4G0Ko7MM2IGC-1JAfI4Q551uIH2rIa9N4YZazLvJfVluvnvNAxhlIcUQaiIDKjkSrh6gh1QtiHUtr7QUB135HNKFhL0Bc5_Pv-CyLKcRUzi82w7jsJ7n1ZvZlYIFQI8o4xHUNFb6rqZBJGlDbSvU9zOqGM0P1D6x7bYSAJSwCV_I5h28yyoAW6opOiZbjW2WTkhOnxC5PEcvUVpC0GpMS1eoNXNNkVS-ykiba.M7gwOCAw5EyDbrOLN9QceQ"
            ;

        public const string accessTokenSecret = "Q0115160387675JIlGDb3DO7VeXouQiMMKgpfdVWDptJmpE8Ye";
        public const string realmId = "193514690612794";
        public const string appToken = "";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("|  Sign in with Google  |");
            Console.WriteLine("+-----------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();

            Program p = new Program();
            p.doOAuth();
            Console.ReadKey();
        }

        // client configuration
        const string clientID = QuickbooksConstants.consumerKey
            ; //"581786658708-elflankerquo1a6vsckabbhn25hclla0.apps.googleusercontent.com";

        const string clientSecret = QuickbooksConstants.consumerSecret; //"3f6NggMbPtrmIBpgx-MK2xXK";
        const string authorizationEndpoint = "https://appcenter.intuit.com/connect/oauth2";
        const string tokenEndpoint = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
        private const string quickbooksURL = "https://sandbox-quickbooks.api.intuit.com/v3/company/193514690615724/";
        private string refresh_token;

        private string access_token;
        private string id_token;

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
                    }

                    List<products> products = ProductDAL.GetProducts(configData, I => I.catalogid == 1004);
                    foreach (var prod in products)
                    {
                        if (prod.qb_product_id != null)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(prod.description))
                        {
                            string response = await CreateItem(quickbooksURL, prod.description);
                            if (!response.StartsWith("error"))
                            {
                                JToken outer = JToken.Parse(response);
                                var pId = outer["Item"]["Id"].Value<int>();
                                prod.qb_product_id = pId;
                                ProductDAL.UpdateProduct(configData, pId, prod.SKU);
                            }
                        }
                    }

                    List<orders> orders = OrderDAL.FetchOrders(configData.ConnectionString,
                        order => order.orderdate >= DateTime.Parse("12/09/2017"));
                    foreach (var o in orders)
                    {
                        if (!string.IsNullOrEmpty(o.orderno))
                        {
                            var customer = customers.FirstOrDefault(I => I.customer_id == o.customerid);
                            if (customer != null && customer.qb_customer_id.HasValue)
                            {
                                string response = await CreateInvoice(quickbooksURL, o, customer.qb_customer_id.Value.ToString(), products);
                                if (!response.StartsWith("error"))
                                {
                                    JToken outer = JToken.Parse(response);
                                    var pId = outer["Invoice"]["Id"].Value<int>();
                                    //o.qb_product_id = pId;
                                    //ProductDAL.UpdateProduct(configData, pId, prod.SKU);
                                }
                            }
                        }
                    }
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
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        public static byte[] sha256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
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

        class LineItem
        {
            public double? Amount { get; set; }
            public string DetailType { get; set; }
            public SalesItemLineDetail SalesItemLineDetail { get; set; }

        }
        class SalesItemLineDetail
        {
            public ItemRef ItemRef { get; set; }
            public TaxCodeRef TaxCodeRef { get; set; }
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
        public async Task<string> CreateInvoice(string url, orders order, 
            string qbCustomerId, bool nonTaxable, List<products> products)
        {
            List<LineItem> objList = new List<LineItem>();
            foreach (var item in order.order_items)
            {
                var prod = products.FirstOrDefault(I => I.catalogid == item.catalogid);
                
                objList.Add(
                    new LineItem
                    {
                        Amount = item.optionprice * item.numitems,
                        DetailType = "SalesItemLineDetail",
                        SalesItemLineDetail = new SalesItemLineDetail
                        {
                            ItemRef = new ItemRef
                            {
                                value = prod.qb_product_id.ToString(),
                                name = prod.description.Substring(0, 100)
                            },
                            TaxCodeRef = new TaxCodeRef
                            {
                                value = nonTaxable ? "NON" : "TAX"
                            }
                        }
                    }
                );
            }
            
            var invoice = new
            {
                Line = objList,
                CustomerRef = new
                {
                    value = qbCustomerId
                }
            };
            string response = await RestAPI("salesreceipt?minorversion=4", JsonConvert.SerializeObject(invoice));
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
                DisplayName = cus.billing_firstname + " " + cus.billing_lastname,
                PrimaryPhone = new
                {
                    FreeFormNumber = cus.billing_phone
                },
                PrimaryEmailAddr = new
                {
                    Address = cus.email
                }
            };
            string response = await RestAPI("customer?minorversion=4", JsonConvert.SerializeObject(customer));
            return response;
        }

        public async System.Threading.Tasks.Task<string> CreateItem(string url, string desc)
        {
            var item = new
            {
                Name = desc.Substring(0, 100),
                IncomeAccountRef = new
                {
                    value = "128",
                    name = "Sales of Product Income"
                },
                ExpenseAccountRef = new
                {
                    value = "168",
                    name = "Cost of Goods Sold"
                },
                //AssetAccountRef = new
                //{
                //    value = "130",
                //    name = "Inventory Asset"
                //},
                Type = "NonInventory",
                TrackQtyOnHand = false,
                //QtyOnHand = 10,
                //InvStartDate = "2015-01-01"
            };
            string response = await RestAPI("item?minorversion=4", JsonConvert.SerializeObject(item));
            return response;
        }
        private async Task<string> RestAPI(string url, string data)
        {
            string responseText = null;
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create("https://sandbox-quickbooks.api.intuit.com/v3/company/193514690615724/" + url);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/json";
            tokenRequest.Accept = "application/json";
            tokenRequest.Headers[HttpRequestHeader.Authorization] = "Bearer " + access_token;
            using (Stream requestStream = tokenRequest.GetRequestStream())
            using (StreamWriter writer = new StreamWriter(requestStream))
            {
                writer.Write(data);
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
    }
}
