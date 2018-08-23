using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;

namespace AmazonApp.Helper
{
    public class FeedRequestXML
    {
        internal static Stream AddStringToStream(ref string s, MemoryStream stream)
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
                myString.AppendLine($"<MessageID>{i + 1}</MessageID>");
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
                        myString.AppendLine($"<StandardPrice currency=\"USD\">{m.SalePrice:N2}</StandardPrice>");
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

        public static Stream GenerateOrderFulfillmentFeed(string merchantID, List<Dictionary<string, object>> messagesDict)
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
            myString.AppendLine($"<MessageType>OrderFulfillment</MessageType>");

            int i = 1;
            foreach (var m in messagesDict)
            {
                myString.AppendLine("<Message>");
                myString.AppendLine($"<MessageID>{i}</MessageID>");
                myString.AppendLine($"<OrderFulfillment>");
                myString.AppendLine($"<AmazonOrderID>{m["AmazonOrderId"]}</AmazonOrderID>");
                myString.AppendLine("<MerchantFulfillmentID>" + (625 + i) + "</MerchantFulfillmentID>");
                myString.AppendLine($"<FulfillmentDate>{m["ShippedDate"]:yyyy-MM-ddT00:00:01}</FulfillmentDate>");

                myString.AppendLine("<FulfillmentData>");
                myString.AppendLine($"<CarrierCode>{m["CarrierCode"]}</CarrierCode>");
                myString.AppendLine($"<ShippingMethod>{m["ShippingMethod"]}</ShippingMethod>");
                myString.AppendLine($"<ShipperTrackingNumber>{m["TrackingNo"]}</ShipperTrackingNumber>");
                myString.AppendLine($"</FulfillmentData>");

                myString.AppendLine("<Item>");
                myString.AppendLine($"<AmazonOrderItemCode>{m["AmazonOrderItemCode"]}</AmazonOrderItemCode>");
                myString.AppendLine($"<MerchantFulfillmentItemID>{300 + i}</MerchantFulfillmentItemID>");
                myString.AppendLine($"<Quantity>{m["Quantity"]}</Quantity>");
                myString.AppendLine("</Item>");
                myString.AppendLine("</OrderFulfillment>");
                myString.AppendLine("</Message>");
                i++;
            }
            myString.AppendLine("</AmazonEnvelope>");
            string newString = myString.ToString();
            AddStringToStream(ref newString, myDocument);
            return myDocument;
        }
    }
}
