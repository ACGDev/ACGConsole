using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp.Model
{
    public class TaxClassification : AbstractMwsObject
    {
        private string _name;
        private string _value;

        /// <summary>
        /// Gets and sets the Name property.
        /// </summary>
        public string Name
        {
            get { return this._name; }
            set { this._name = value; }
        }

        /// <summary>
        /// Sets the Name property.
        /// </summary>
        /// <param name="name">Name property.</param>
        /// <returns>this instance.</returns>
        public TaxClassification WithName(string name)
        {
            this._name = name;
            return this;
        }

        /// <summary>
        /// Checks if Name property is set.
        /// </summary>
        /// <returns>true if Name property is set.</returns>
        public bool IsSetName()
        {
            return this._name != null;
        }

        /// <summary>
        /// Gets and sets the Value property.
        /// </summary>
        public string Value
        {
            get { return this._value; }
            set { this._value = value; }
        }

        /// <summary>
        /// Sets the Value property.
        /// </summary>
        /// <param name="value">Value property.</param>
        /// <returns>this instance.</returns>
        public TaxClassification WithValue(string value)
        {
            this._value = value;
            return this;
        }

        /// <summary>
        /// Checks if Value property is set.
        /// </summary>
        /// <returns>true if Value property is set.</returns>
        public bool IsSetValue()
        {
            return this._value != null;
        }


        public override void ReadFragmentFrom(IMwsReader reader)
        {
            _name = reader.Read<string>("Name");
            _value = reader.Read<string>("Value");
        }

        public override void WriteFragmentTo(IMwsWriter writer)
        {
            writer.Write("Name", _name);
            writer.Write("Value", _value);
        }

        public override void WriteTo(IMwsWriter writer)
        {
            writer.Write("https://mws.amazonservices.com/Orders/2013-09-01", "TaxClassification", this);
        }


        public TaxClassification() : base()
        {
        }
    }
}
