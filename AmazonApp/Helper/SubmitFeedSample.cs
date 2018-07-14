using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazonApp.Model;

namespace AmazonApp.Helper
{
    public class SubmitFeedSample
    {


        /// <summary>
        /// Uploads a file for processing together with the necessary
        /// metadata to process the file, such as which type of feed it is.
        /// PurgeAndReplace if true means that your existing e.g. inventory is
        /// wiped out and replace with the contents of this feed - use with
        /// caution (the default is false).
        /// 
        /// </summary>
        /// <param name="service">Instance of MarketplaceWebService service</param>
        /// <param name="request">SubmitFeedRequest request</param>
        public static void InvokeSubmitFeed(MarketplaceWebService service, SubmitFeedRequest request)
        {
            try
            {
                SubmitFeedResponse response = service.SubmitFeed(request);


                Console.WriteLine("Service Response");
                Console.WriteLine("=============================================================================");
                Console.WriteLine();

                Console.WriteLine("        SubmitFeedResponse");
                if (response.IsSetSubmitFeedResult())
                {
                    Console.WriteLine("            SubmitFeedResult");
                    SubmitFeedResult submitFeedResult = response.SubmitFeedResult;
                    if (submitFeedResult.IsSetFeedSubmissionInfo())
                    {
                        Console.WriteLine("                FeedSubmissionInfo");
                        FeedSubmissionInfo feedSubmissionInfo = submitFeedResult.FeedSubmissionInfo;
                        if (feedSubmissionInfo.IsSetFeedSubmissionId())
                        {
                            Console.WriteLine("                    FeedSubmissionId");
                            Console.WriteLine("                        {0}", feedSubmissionInfo.FeedSubmissionId);
                        }
                        if (feedSubmissionInfo.IsSetFeedType())
                        {
                            Console.WriteLine("                    FeedType");
                            Console.WriteLine("                        {0}", feedSubmissionInfo.FeedType);
                        }
                        if (feedSubmissionInfo.IsSetSubmittedDate())
                        {
                            Console.WriteLine("                    SubmittedDate");
                            Console.WriteLine("                        {0}", feedSubmissionInfo.SubmittedDate);
                        }
                        if (feedSubmissionInfo.IsSetFeedProcessingStatus())
                        {
                            Console.WriteLine("                    FeedProcessingStatus");
                            Console.WriteLine("                        {0}", feedSubmissionInfo.FeedProcessingStatus);
                        }
                        if (feedSubmissionInfo.IsSetStartedProcessingDate())
                        {
                            Console.WriteLine("                    StartedProcessingDate");
                            Console.WriteLine("                        {0}", feedSubmissionInfo.StartedProcessingDate);
                        }
                        if (feedSubmissionInfo.IsSetCompletedProcessingDate())
                        {
                            Console.WriteLine("                    CompletedProcessingDate");
                            Console.WriteLine("                        {0}", feedSubmissionInfo.CompletedProcessingDate);
                        }
                    }
                }
                if (response.IsSetResponseMetadata())
                {
                    Console.WriteLine("            ResponseMetadata");
                    ResponseMetadata responseMetadata = response.ResponseMetadata;
                    if (responseMetadata.IsSetRequestId())
                    {
                        Console.WriteLine("                RequestId");
                        Console.WriteLine("                    {0}", responseMetadata.RequestId);
                    }
                }

                Console.WriteLine("            ResponseHeaderMetadata");
                Console.WriteLine("                RequestId");
                Console.WriteLine("                    " + response.ResponseHeaderMetadata2.RequestId);
                Console.WriteLine("                ResponseContext");
                Console.WriteLine("                    " + response.ResponseHeaderMetadata2.ResponseContext);
                Console.WriteLine("                Timestamp");
                Console.WriteLine("                    " + response.ResponseHeaderMetadata2.Timestamp);

            }
            catch (MarketplaceWebServiceException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
                Console.WriteLine("Response Status Code: " + ex.StatusCode);
                Console.WriteLine("Error Code: " + ex.ErrorCode);
                Console.WriteLine("Error Type: " + ex.ErrorType);
                Console.WriteLine("Request ID: " + ex.RequestId);
                Console.WriteLine("XML: " + ex.XML);
                Console.WriteLine("ResponseHeaderMetadata: " + ex.ResponseHeaderMetadata);
            }
        }
    }
}
