﻿/******************************************************************************* 
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AmazonApp.Model
{
    [XmlType(Namespace = "http://mws.amazonaws.com/doc/2009-01-01/")]
    [XmlRootAttribute(Namespace = "http://mws.amazonaws.com/doc/2009-01-01/", IsNullable = false)]
    public class GetFeedSubmissionResultResult
    {

        private String contentMd5;

        [XmlElementAttribute(ElementName = "ContentMD5")]
        public String ContentMD5
        {
            set { this.contentMd5 = value; }
            get { return this.contentMd5; }
        }

        public Boolean IsSetContentMD5()
        {
            return this.contentMd5 != null;
        }

        /// <summary>
        /// XML fragment representation of this object
        /// </summary>
        /// <returns>XML fragment for this object.</returns>
        /// <remarks>
        /// Name for outer tag expected to be set by calling method. 
        /// This fragment returns inner properties representation only
        /// </remarks>


        protected internal String ToXMLFragment()
        {
            StringBuilder xml = new StringBuilder();
            return xml.ToString();
        }

        /**
         * 
         * Escape XML special characters
         */
        private String EscapeXML(String str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Char c in str)
            {
                switch (c)
                {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '\'':
                        sb.Append("&#039;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
