using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp.Helper
{
    class FeedRequestXML
    {
        private static Stream AddStringToStream(ref string s, MemoryStream stream)
        {
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        internal static Stream GenerateInventoryDocument(string merchantID, string sku, string asin, string type, int quantity = 0, int fulfillmentLatency = 0, double price = 0)
        {
            MemoryStream myDocument = new MemoryStream();
            StringBuilder myString = new StringBuilder();

            //Add the document header.
            myString.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            myString.AppendLine("<AmazonEnvelope xsi:noNamespaceSchemaLocation=\"amzn-envelope.xsd\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
            myString.AppendLine("<Header>");
            myString.AppendLine("<DocumentVersion>1.01</DocumentVersion>");
            myString.AppendLine("<MerchantIdentifier>" + merchantID + "</MerchantIdentifier>");
            myString.AppendLine("</Header>");
            myString.AppendLine($"<MessageType>{type}</MessageType>");
            if (type == "Product")
            {
                myString.AppendLine("<PurgeAndReplace>false</PurgeAndReplace>");
            }
            myString.AppendLine("<Message>");
            myString.AppendLine("<MessageID>1</MessageID>");
            myString.AppendLine("<OperationType>Update</OperationType>");
            myString.AppendLine($"<{type}>");
            myString.AppendLine("<SKU>" + sku + "</SKU>");
            if (type == "Product")
            {
                myString.AppendLine("<StandardProductID>");
                myString.AppendLine("<Type>ASIN</Type>");
                myString.AppendLine("<Value>" + asin + "</Value>");
                myString.AppendLine($"</StandardProductID>");
                myString.AppendLine($"<LaunchDate>2018-07-13T00:00:01</LaunchDate>");
            }
            else if (type == "Inventory")
            {
                myString.AppendLine($"<Quantity>{quantity}</Quantity>");
                myString.AppendLine($"<FulfillmentLatency>{fulfillmentLatency}</FulfillmentLatency>");
            }
            else
            {
                myString.AppendLine($"<StandardPrice currency=\"USD\">{price}</StandardPrice>");
            }
            myString.AppendLine($"</{type}>");
            myString.AppendLine("</Message>");
            myString.AppendLine("</AmazonEnvelope>");
            string newString = myString.ToString();
            int length = newString.Length;
            AddStringToStream(ref newString, myDocument);
            return myDocument;
        }

    }
}
