/*******************************************************************************
 * Copyright 2009-2018 Amazon Services. All Rights Reserved.
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 *
 * You may not use this file except in compliance with the License. 
 * You may obtain a copy of the License at: http://aws.amazon.com/apache2.0
 * This file is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the 
 * specific language governing permissions and limitations under the License.
 *******************************************************************************
 * Tax Collection
 * API Version: 2013-09-01
 * Library Version: 2018-01-31
 * Generated: Tue Jan 30 16:03:19 PST 2018
 */

using System;
using System.Xml;

namespace AmazonApp.Model
{
    public class TaxCollection : AbstractMwsObject
    {
        private string _model;
        private string _responsibleParty;

        /// <summary>
        /// Gets and sets the Model property.
        /// </summary>
        public string Model
        {
            get { return this._model; }
            set { this._model = value; }
        }

        /// <summary>
        /// Sets the Model property.
        /// </summary>
        /// <param name="model">Model property.</param>
        /// <returns>this instance.</returns>
        public TaxCollection WithModel(string model)
        {
            this._model = model;
            return this;
        }

        /// <summary>
        /// Checks if Model property is set.
        /// </summary>
        /// <returns>true if Model property is set.</returns>
        public bool IsSetModel()
        {
            return this._model != null;
        }

        /// <summary>
        /// Gets and sets the ResponsibleParty property.
        /// </summary>
        public string ResponsibleParty
        {
            get { return this._responsibleParty; }
            set { this._responsibleParty = value; }
        }

        /// <summary>
        /// Sets the ResponsibleParty property.
        /// </summary>
        /// <param name="responsibleParty">ResponsibleParty property.</param>
        /// <returns>this instance.</returns>
        public TaxCollection WithResponsibleParty(string responsibleParty)
        {
            this._responsibleParty = responsibleParty;
            return this;
        }

        /// <summary>
        /// Checks if ResponsibleParty property is set.
        /// </summary>
        /// <returns>true if ResponsibleParty property is set.</returns>
        public bool IsSetResponsibleParty()
        {
            return this._responsibleParty != null;
        }


        public override void ReadFragmentFrom(IMwsReader reader)
        {
            _model = reader.Read<string>("Model");
            _responsibleParty = reader.Read<string>("ResponsibleParty");
        }

        public override void WriteFragmentTo(IMwsWriter writer)
        {
            writer.Write("Model", _model);
            writer.Write("ResponsibleParty", _responsibleParty);
        }

        public override void WriteTo(IMwsWriter writer)
        {
            writer.Write("https://mws.amazonservices.com/Orders/2013-09-01", "TaxCollection", this);
        }


        public TaxCollection() : base()
        {
        }
    }
}
