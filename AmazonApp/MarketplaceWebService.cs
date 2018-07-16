/******************************************************************************* 
 *  Copyright 2009 Amazon Services.
 *  Licensed under the Apache License, Version 2.0 (the "License"); 
 *  
 *  You may not use this file except in compliance with the License. 
 *  You may obtain a copy of the License at: http://aws.amazon.com/apache2.0
 *  This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
 *  CONDITIONS OF ANY KIND, either express or implied. See the License for the 
 *  specific language governing permissions and limitations under the License.
 * ***************************************************************************** 
 * 
 *  Marketplace Web Service CSharp Library
 *  API Version: 2009-01-01
 *  Generated: Mon Mar 16 17:31:42 PDT 2009 
 * 
 */

using AmazonApp.Model;

namespace AmazonApp
{
    /// <summary>
    /// The Amazon Marketplace Web Service contain APIs for inventory and order management.
    /// modified: amazon code
    /// </summary>
    public interface  MarketplaceWebService {
    
                
        /// <summary>
        /// Submit Feed 
        /// </summary>
        /// <param name="request">Submit Feed  request</param>
        /// <returns>Submit Feed  Response from the service</returns>
        /// <remarks>
        /// Uploads a file for processing together with the necessary
        /// metadata to process the file, such as which type of feed it is.
        /// PurgeAndReplace if true means that your existing e.g. inventory is
        /// wiped out and replace with the contents of this feed - use with
        /// caution (the default is false).
        ///   
        /// </remarks>
        SubmitFeedResponse SubmitFeed(SubmitFeedRequest request);

        /// <summary>
        /// Get Feed Submission Result 
        /// </summary>
        /// <param name="request">Get Feed Submission Result  request</param>
        /// <returns>Get Feed Submission Result  Response from the service</returns>
        /// <remarks>
        /// retrieves the feed processing report
        ///   
        /// </remarks>
        GetFeedSubmissionResultResponse GetFeedSubmissionResult(GetFeedSubmissionResultRequest request);
    }
}
