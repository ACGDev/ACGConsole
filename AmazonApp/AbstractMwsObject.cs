using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonApp
{
    /// <summary>
    /// Abstract implementation of MwsObject
    /// </summary>
    public abstract class AbstractMwsObject : IMwsObject
    {

        public string ToXML()
        {
            IMwsWriter writer = new MwsXmlBuilder();
            this.WriteTo(writer);
            return writer.ToString();
        }

        public string ToXMLFragment()
        {
            IMwsWriter writer = new MwsXmlBuilder(false, System.Xml.ConformanceLevel.Fragment);
            this.WriteFragmentTo(writer);
            return writer.ToString();
        }

        public abstract void ReadFragmentFrom(IMwsReader r);

        public abstract void WriteFragmentTo(IMwsWriter w);

        public abstract void WriteTo(IMwsWriter w);

    }
}
