using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmazonApp.Model;

namespace AmazonApp
{
    public class ResponseHeaderMetadata : MwsResponseHeaderMetadata
    {
        public ResponseHeaderMetadata(string requestId, string responseContext, string timestamp, double? quotaMax, double? quotaRemaining, DateTime? quotaResetsAt)
            : base(requestId, responseContext, timestamp, quotaMax, quotaRemaining, quotaResetsAt) { }

        public ResponseHeaderMetadata()
            : base(null, "", null, null, null, null) { }

        public ResponseHeaderMetadata(MwsResponseHeaderMetadata rhmd)
            : base(rhmd) { }
    }
    public interface IMWSResponse : IMwsObject
    {
        ResponseHeaderMetadata ResponseHeaderMetadata
        {
            get;
            set;
        }

    }

    public class ListOrdersResponse : AbstractMwsObject, IMWSResponse
    {
        private ListOrdersResult _listOrdersResult;
        private ResponseMetadata _responseMetadata;
        private ResponseHeaderMetadata _responseHeaderMetadata;

        /// <summary>
        /// Gets and sets the ListOrdersResult property.
        /// </summary>
        public ListOrdersResult ListOrdersResult
        {
            get { return this._listOrdersResult; }
            set { this._listOrdersResult = value; }
        }

        /// <summary>
        /// Sets the ListOrdersResult property.
        /// </summary>
        /// <param name="listOrdersResult">ListOrdersResult property.</param>
        /// <returns>this instance.</returns>
        public ListOrdersResponse WithListOrdersResult(ListOrdersResult listOrdersResult)
        {
            this._listOrdersResult = listOrdersResult;
            return this;
        }

        /// <summary>
        /// Checks if ListOrdersResult property is set.
        /// </summary>
        /// <returns>true if ListOrdersResult property is set.</returns>
        public bool IsSetListOrdersResult()
        {
            return this._listOrdersResult != null;
        }

        /// <summary>
        /// Gets and sets the ResponseMetadata property.
        /// </summary>
        public ResponseMetadata ResponseMetadata
        {
            get { return this._responseMetadata; }
            set { this._responseMetadata = value; }
        }

        /// <summary>
        /// Sets the ResponseMetadata property.
        /// </summary>
        /// <param name="responseMetadata">ResponseMetadata property.</param>
        /// <returns>this instance.</returns>
        public ListOrdersResponse WithResponseMetadata(ResponseMetadata responseMetadata)
        {
            this._responseMetadata = responseMetadata;
            return this;
        }

        /// <summary>
        /// Checks if ResponseMetadata property is set.
        /// </summary>
        /// <returns>true if ResponseMetadata property is set.</returns>
        public bool IsSetResponseMetadata()
        {
            return this._responseMetadata != null;
        }

        /// <summary>
        /// Gets and sets the ResponseHeaderMetadata property.
        /// </summary>
        public ResponseHeaderMetadata ResponseHeaderMetadata
        {
            get { return this._responseHeaderMetadata; }
            set { this._responseHeaderMetadata = value; }
        }

        /// <summary>
        /// Sets the ResponseHeaderMetadata property.
        /// </summary>
        /// <param name="responseHeaderMetadata">ResponseHeaderMetadata property.</param>
        /// <returns>this instance.</returns>
        public ListOrdersResponse WithResponseHeaderMetadata(ResponseHeaderMetadata responseHeaderMetadata)
        {
            this._responseHeaderMetadata = responseHeaderMetadata;
            return this;
        }

        /// <summary>
        /// Checks if ResponseHeaderMetadata property is set.
        /// </summary>
        /// <returns>true if ResponseHeaderMetadata property is set.</returns>
        public bool IsSetResponseHeaderMetadata()
        {
            return this._responseHeaderMetadata != null;
        }


        public override void ReadFragmentFrom(IMwsReader reader)
        {
            _listOrdersResult = reader.Read<ListOrdersResult>("ListOrdersResult");
            _responseMetadata = reader.Read<ResponseMetadata>("ResponseMetadata");
        }

        public override void WriteFragmentTo(IMwsWriter writer)
        {
            writer.Write("ListOrdersResult", _listOrdersResult);
            writer.Write("ResponseMetadata", _responseMetadata);
        }

        public override void WriteTo(IMwsWriter writer)
        {
            writer.Write("https://mws.amazonservices.com/Orders/2013-09-01", "ListOrdersResponse", this);
        }


        public ListOrdersResponse() : base()
        {
        }
    }
}
