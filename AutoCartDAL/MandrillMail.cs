using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;
using AutoCarOperations.DAL;
using Mandrill;
using Mandrill.Model;
using System.IO;

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
        public static void SendEmail(string apiKey, string header, string body, string emailTo,string filefullname, string filename, string extension)
        {
            /*
             * Uncomment namespace from top
             * <add key="MandrilAPIKey" value="fake"/>
             */
            var api = new MandrillApi(apiKey);
            var message = new MandrillMessage(CommonConstant.EmailFrom, emailTo,
                header, body);
            //  add the file as attachment
            message.Attachments.Add(new MandrillAttachment(extension, filename, FileToByteArray(filefullname)));
            var result = api.Messages.SendAsync(message);
            result.Wait();
            var checkResult = result.Result;
        }
        public static void SendEmail(string apiKey, string header, string body, string emailTo)
        {
            /*
             * Uncomment namespace from top
             * <add key="MandrilAPIKey" value="fake"/>
             */
            var api = new MandrillApi(apiKey);
            var message = new MandrillMessage(CommonConstant.EmailFrom, emailTo,
                header, body);
            var result = api.Messages.SendAsync(message);
            result.Wait();
            var checkResult = result.Result;
        }
        public static byte[] FileToByteArray(string fileName)
        {
            byte[] fileData = null;

            using (FileStream fs = File.OpenRead(fileName))
            {
                var binaryReader = new BinaryReader(fs);
                fileData = binaryReader.ReadBytes((int)fs.Length);
            }
            return fileData;
        }
    }
}
