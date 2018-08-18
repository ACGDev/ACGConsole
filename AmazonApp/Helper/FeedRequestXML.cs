using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;

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

        internal static Stream GenerateInventoryDocument(string merchantID, List<FeedModel> messages, string type)
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
            int i = 0;
            foreach (var m in messages)
            {
                if (i == 0)
                {
                    myString.AppendLine($"<MessageType>{type}</MessageType>");
                    if (type == "Product")
                    {
                        myString.AppendLine("<PurgeAndReplace>false</PurgeAndReplace>");
                    }
                }
                myString.AppendLine("<Message>");
                myString.AppendLine($"<MessageID>{i+1}</MessageID>");
                myString.AppendLine("<OperationType>Update</OperationType>");
                myString.AppendLine($"<{type}>");
                myString.AppendLine("<SKU>" + m.sku + "</SKU>");
                switch (type)
                {
                    case "Product":
                        myString.AppendLine("<StandardProductID>");
                        myString.AppendLine("<Type>ASIN</Type>");
                        myString.AppendLine("<Value>" + m.ASIN + "</Value>");
                        myString.AppendLine($"</StandardProductID>");
                        myString.AppendLine($"<LaunchDate>{DateTime.Now:yyyy-MM-ddT00:00:01}</LaunchDate>");
                        break;
                    case "Inventory":
                        myString.AppendLine($"<Quantity>{m.InventoryQty}</Quantity>");
                        myString.AppendLine($"<FulfillmentLatency>{m.HandlingTime}</FulfillmentLatency>");
                        break;
                    default:
                        myString.AppendLine($"<StandardPrice currency=\"USD\">{m.SalePrice}</StandardPrice>");
                        break;
                }
                myString.AppendLine($"</{type}>");
                myString.AppendLine("</Message>");
                i++;
            }
            myString.AppendLine("</AmazonEnvelope>");
            string newString = myString.ToString();
            int length = newString.Length;
            AddStringToStream(ref newString, myDocument);
            return myDocument;
        }

    }
}
