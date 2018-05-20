using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AmazonApp
{
    public class MwsXmlBuilder : MwsXmlWriter
    {
        StringBuilder builder;
        public MwsXmlBuilder(bool toWrap)
        {
            builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = !toWrap;
            writer = XmlWriter.Create(builder, settings);
        }


        public MwsXmlBuilder() : this(false) { }

        public MwsXmlBuilder(bool toWrap, ConformanceLevel conformanceLevel)
        {
            builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = !toWrap;
            settings.ConformanceLevel = conformanceLevel;
            writer = XmlWriter.Create(builder, settings);
        }

        public override string ToString()
        {
            writer.Flush();
            return builder.ToString(0, builder.Length);
        }

    }
}
