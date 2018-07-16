﻿using System;
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

        internal static Stream GenerateInventoryDocument(string merchantID, List<FeedModel> messages)
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
                    myString.AppendLine($"<MessageType>{m.type}</MessageType>");
                    if (m.type == "Product")
                    {
                        myString.AppendLine("<PurgeAndReplace>false</PurgeAndReplace>");
                    }
                }
                myString.AppendLine("<Message>");
                myString.AppendLine($"<MessageID>{i+1}</MessageID>");
                myString.AppendLine("<OperationType>Update</OperationType>");
                myString.AppendLine($"<{m.type}>");
                myString.AppendLine("<SKU>" + m.sku + "</SKU>");
                switch (m.type)
                {
                    case "Product":
                        myString.AppendLine("<StandardProductID>");
                        myString.AppendLine("<Type>ASIN</Type>");
                        myString.AppendLine("<Value>" + m.asin + "</Value>");
                        myString.AppendLine($"</StandardProductID>");
                        myString.AppendLine($"<LaunchDate>{DateTime.Now:yyyy-MM-ddT00:00:01}</LaunchDate>");
                        break;
                    case "Inventory":
                        myString.AppendLine($"<Quantity>{m.quantity}</Quantity>");
                        myString.AppendLine($"<FulfillmentLatency>{m.fulfillmentLatency}</FulfillmentLatency>");
                        break;
                    default:
                        myString.AppendLine($"<StandardPrice currency=\"USD\">{m.price}</StandardPrice>");
                        break;
                }
                myString.AppendLine($"</{m.type}>");
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
