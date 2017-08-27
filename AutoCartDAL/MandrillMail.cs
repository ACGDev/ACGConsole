using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;
using AutoCarOperations.DAL;
using Mandrill;
using Mandrill.Model;

namespace AutoCarOperations
{
    public class MandrillMail
    {
        /// <summary>
        /// todo: not used anywhere
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="header"></param>
        /// <param name="body"></param>
        /// <param name="emailTo"></param>
        public static void SendEmail(string apiKey, string header, string body, string emailTo)
        {
            /*
             * Uncomment namespace from top
             * <add key="MandrilAPIKey" value="fake"/>
             */
            var api = new MandrillApi(apiKey);
            var message = new MandrillMessage(emailTo, CommonConstant.EmailFrom,
                header, body);
            var result = api.Messages.SendAsync(message);
            result.Wait();
            var checkResult = result.Result;
        }
    }
}
