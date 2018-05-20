using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp.Model
{
    public class GetOrderResult : AbstractMwsObject
    {
        private List<Order> _orders;

        /// <summary>
        /// Gets and sets the Orders property.
        /// </summary>
        public List<Order> Orders
        {
            get
            {
                if (this._orders == null)
                {
                    this._orders = new List<Order>();
                }
                return this._orders;
            }
            set { this._orders = value; }
        }

        /// <summary>
        /// Sets the Orders property.
        /// </summary>
        /// <param name="orders">Orders property.</param>
        /// <returns>this instance.</returns>
        public GetOrderResult WithOrders(Order[] orders)
        {
            this._orders.AddRange(orders);
            return this;
        }

        /// <summary>
        /// Checks if Orders property is set.
        /// </summary>
        /// <returns>true if Orders property is set.</returns>
        public bool IsSetOrders()
        {
            return this.Orders.Count > 0;
        }


        public override void ReadFragmentFrom(IMwsReader reader)
        {
            _orders = reader.ReadList<Order>("Orders", "Order");
        }

        public override void WriteFragmentTo(IMwsWriter writer)
        {
            writer.WriteList("Orders", "Order", _orders);
        }

        public override void WriteTo(IMwsWriter writer)
        {
            writer.Write("https://mws.amazonservices.com/Orders/2013-09-01", "GetOrderResult", this);
        }

        public GetOrderResult(List<Order> orders) : base()
        {
            this._orders = orders;
        }

        public GetOrderResult() : base()
        {
        }
    }
}
