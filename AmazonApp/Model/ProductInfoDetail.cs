using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp.Model
{
    public class ProductInfoDetail : AbstractMwsObject
    {
        private decimal? _numberOfItems;

        /// <summary>
        /// Gets and sets the NumberOfItems property.
        /// </summary>
        public decimal NumberOfItems
        {
            get { return this._numberOfItems.GetValueOrDefault(); }
            set { this._numberOfItems = value; }
        }

        /// <summary>
        /// Sets the NumberOfItems property.
        /// </summary>
        /// <param name="numberOfItems">NumberOfItems property.</param>
        /// <returns>this instance.</returns>
        public ProductInfoDetail WithNumberOfItems(decimal numberOfItems)
        {
            this._numberOfItems = numberOfItems;
            return this;
        }

        /// <summary>
        /// Checks if NumberOfItems property is set.
        /// </summary>
        /// <returns>true if NumberOfItems property is set.</returns>
        public bool IsSetNumberOfItems()
        {
            return this._numberOfItems != null;
        }


        public override void ReadFragmentFrom(IMwsReader reader)
        {
            _numberOfItems = reader.Read<decimal?>("NumberOfItems");
        }

        public override void WriteFragmentTo(IMwsWriter writer)
        {
            writer.Write("NumberOfItems", _numberOfItems);
        }

        public override void WriteTo(IMwsWriter writer)
        {
            writer.Write("https://mws.amazonservices.com/Orders/2013-09-01", "ProductInfoDetail", this);
        }


        public ProductInfoDetail() : base()
        {
        }
    }
}
