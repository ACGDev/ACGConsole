﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazonApp.Model;

namespace AmazonApp.Helper
{
    public class FeedSample
    {

        /// <summary>
        /// retrieves the feed processing report
        /// 
        /// </summary>
        /// <param name="service">Instance of MarketplaceWebService service</param>
        /// <param name="request">GetFeedSubmissionResultRequest request</param>
        public static GetFeedSubmissionResultResponse InvokeGetFeedSubmissionResult(MarketplaceWebService service, GetFeedSubmissionResultRequest request)
        {
            try
            {
                GetFeedSubmissionResultResponse response = service.GetFeedSubmissionResult(request);

                string re = response.ToXML();
                Console.WriteLine("Service Response");
                Console.WriteLine("=============================================================================");
                Console.WriteLine();

                Console.WriteLine("        GetFeedSubmissionResultResponse");
                if (response.IsSetGetFeedSubmissionResultResult())
                {
                    Console.WriteLine("            GetFeedSubmissionResult");
                    GetFeedSubmissionResultResult getFeedSubmissionResultResult = response.GetFeedSubmissionResultResult;
                    //if (getFeedSubmissionResultResult.IsSetContentMD5())
                    //{
                    //    Console.WriteLine("                ContentMD5");
                    //    Console.WriteLine("                    {0}", getFeedSubmissionResultResult.ContentMD5);
                    //}
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

                //Console.WriteLine("            ResponseHeaderMetadata");
                //Console.WriteLine("                RequestId");
                //Console.WriteLine("                    " + response.ResponseHeaderMetadata2.RequestId);
                //Console.WriteLine("                ResponseContext");
                //Console.WriteLine("                    " + response.ResponseHeaderMetadata2.ResponseContext);
                //Console.WriteLine("                Timestamp");
                //Console.WriteLine("                    " + response.ResponseHeaderMetadata2.Timestamp);
                return response;
            }
            catch (MarketplaceWebServiceException ex)
            {
                if (ex.ErrorCode == "FeedProcessingResultNotReady")
                {
                    Console.WriteLine("{0} {1} ", ex.Message,DateTime.Now);
                }
                else
                {
                    Console.WriteLine("Caught Exception: " + ex.Message);
                    Console.WriteLine("Response Status Code: " + ex.StatusCode);
                    Console.WriteLine("Error Code: " + ex.ErrorCode);
                    Console.WriteLine("Error Type: " + ex.ErrorType);
                    Console.WriteLine("Request ID: " + ex.RequestId);
                    Console.WriteLine("XML: " + ex.XML);
                    Console.WriteLine("ResponseHeaderMetadata: " + ex.ResponseHeaderMetadata);
                }
                return null;
            }
        }

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
        public static SubmitFeedResponse InvokeSubmitFeed(MarketplaceWebService service, SubmitFeedRequest request)
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
                            Console.WriteLine("                FeedSubmissionId {0}", feedSubmissionInfo.FeedSubmissionId);         
                        }
                        if (feedSubmissionInfo.IsSetFeedType())
                        {
                            Console.WriteLine("                    FeedType  {0}", feedSubmissionInfo.FeedType);                  
                        }
                        if (feedSubmissionInfo.IsSetSubmittedDate())
                        {
                            Console.WriteLine("                    SubmittedDate {0}", feedSubmissionInfo.SubmittedDate);
                        }
                        if (feedSubmissionInfo.IsSetFeedProcessingStatus())
                        {
                            Console.WriteLine("                    FeedProcessingStatus {0}", feedSubmissionInfo.FeedProcessingStatus);               
                        }
                        if (feedSubmissionInfo.IsSetStartedProcessingDate())
                        {
                            Console.WriteLine("                    StartedProcessingDate {0}", feedSubmissionInfo.StartedProcessingDate);
                        }
                        if (feedSubmissionInfo.IsSetCompletedProcessingDate())
                        {
                            Console.WriteLine("                    CompletedProcessingDate {0}",feedSubmissionInfo.CompletedProcessingDate);
                        }
                    }
                }
                if (response.IsSetResponseMetadata())
                {
                    Console.WriteLine("            ResponseMetadata");
                    ResponseMetadata responseMetadata = response.ResponseMetadata;
                    if (responseMetadata.IsSetRequestId())
                    {
                        Console.WriteLine("                RequestId {0}", responseMetadata.RequestId);             
                    }
                }

                Console.WriteLine("            ResponseHeaderMetadata");
                Console.WriteLine("                RequestId " + response.ResponseHeaderMetadata2.RequestId);
                Console.WriteLine("                ResponseContext " + response.ResponseHeaderMetadata2.ResponseContext);
                Console.WriteLine("                Timestamp " + response.ResponseHeaderMetadata2.Timestamp);
                return response;
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
            return null;
        }
    }
}
