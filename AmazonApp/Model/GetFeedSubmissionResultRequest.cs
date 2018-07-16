using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AmazonApp.Attributes;

namespace AmazonApp.Model
{
    [XmlType(Namespace = "http://mws.amazonaws.com/doc/2009-01-01/")]
    [XmlRootAttribute(Namespace = "http://mws.amazonaws.com/doc/2009-01-01/", IsNullable = false)]
    [MarketplaceWebService(RequestType = RequestType.DEFAULT, ResponseType = ResponseType.STREAMING)]
    public class GetFeedSubmissionResultRequest
    {

        private String marketplaceField;

        private String merchantField;
        private String mwsAuthTokenField;

        private String feedSubmissionIdField;

        private Stream feedSubmissionResultField;

        [MarketplaceWebServiceStream(StreamType = StreamType.RECEIVE_STREAM)]
        public Stream FeedSubmissionResult
        {
            get { return this.feedSubmissionResultField; }
            set { this.feedSubmissionResultField = value; }
        }

        public GetFeedSubmissionResultRequest WithFeedSubmissionResult(Stream feedSubmissionResult)
        {
            this.feedSubmissionResultField = feedSubmissionResult;
            return this;
        }


        /// <summary>
        /// Gets and sets the Marketplace property.
        /// </summary>
        [XmlElementAttribute(ElementName = "Marketplace")]
        [System.Obsolete("Not used anymore. MWS ignores this parameter, but it is left in here for backwards compatibility.")]
        public String Marketplace
        {
            get { return this.marketplaceField; }
            set { this.marketplaceField = value; }
        }



        /// <summary>
        /// Sets the Marketplace property
        /// </summary>
        /// <param name="marketplace">Marketplace property</param>
        /// <returns>this instance</returns>
        [System.Obsolete("Not used anymore. MWS ignores this parameter, but it is left in here for backwards compatibility.")]
        public GetFeedSubmissionResultRequest WithMarketplace(String marketplace)
        {
            this.marketplaceField = marketplace;
            return this;
        }



        /// <summary>
        /// Checks if Marketplace property is set
        /// </summary>
        /// <returns>true if Marketplace property is set</returns>
        [System.Obsolete("Not used anymore. MWS ignores this parameter, but it is left in here for backwards compatibility.")]
        public Boolean IsSetMarketplace()
        {
            return this.marketplaceField != null;

        }


        /// <summary>
        /// Gets and sets the Merchant property.
        /// </summary>
        [XmlElementAttribute(ElementName = "Merchant")]
        public String Merchant
        {
            get { return this.merchantField; }
            set { this.merchantField = value; }
        }



        /// <summary>
        /// Sets the Merchant property
        /// </summary>
        /// <param name="merchant">Merchant property</param>
        /// <returns>this instance</returns>
        public GetFeedSubmissionResultRequest WithMerchant(String merchant)
        {
            this.merchantField = merchant;
            return this;
        }



        /// <summary>
        /// Checks if Merchant property is set
        /// </summary>
        /// <returns>true if Merchant property is set</returns>
        public Boolean IsSetMerchant()
        {
            return this.merchantField != null;

        }


        /// <summary>
        /// Gets and sets the MWSAuthToken property.
        /// </summary>
        [XmlElementAttribute(ElementName = "MWSAuthToken")]
        public String MWSAuthToken
        {
            get { return this.mwsAuthTokenField; }
            set { this.mwsAuthTokenField = value; }
        }



        /// <summary>
        /// Sets the MWSAuthToken property
        /// </summary>
        /// <param name="mwsAuthToken">MWSAuthToken property</param>
        /// <returns>this instance</returns>
        public GetFeedSubmissionResultRequest WithMWSAuthToken(String mwsAuthToken)
        {
            this.mwsAuthTokenField = mwsAuthToken;
            return this;
        }



        /// <summary>
        /// Checks if MWSAuthToken property is set
        /// </summary>
        /// <returns>true if MWSAuthToken property is set</returns>
        public Boolean IsSetMWSAuthToken()
        {
            return this.mwsAuthTokenField != null;
        }


        /// <summary>
        /// Gets and sets the FeedSubmissionId property.
        /// </summary>
        [XmlElementAttribute(ElementName = "FeedSubmissionId")]
        public String FeedSubmissionId
        {
            get { return this.feedSubmissionIdField; }
            set { this.feedSubmissionIdField = value; }
        }



        /// <summary>
        /// Sets the FeedSubmissionId property
        /// </summary>
        /// <param name="feedSubmissionId">FeedSubmissionId property</param>
        /// <returns>this instance</returns>
        public GetFeedSubmissionResultRequest WithFeedSubmissionId(String feedSubmissionId)
        {
            this.feedSubmissionIdField = feedSubmissionId;
            return this;
        }



        /// <summary>
        /// Checks if FeedSubmissionId property is set
        /// </summary>
        /// <returns>true if FeedSubmissionId property is set</returns>
        public Boolean IsSetFeedSubmissionId()
        {
            return this.feedSubmissionIdField != null;

        }
    }
}
