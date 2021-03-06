﻿/*

Copyright Robert Vesse 2009-11
rvesse@vdesign-studios.com

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

#if UNFINISHED

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing.Contexts;

namespace VDS.RDF.Parsing.Events.RdfA
{
    public class XHtmlHost : BaseHost
    {
        private Uri _baseUri;
        private String _version;

        public override Uri InitialBaseUri
        {
            get
            {
                return this._baseUri;
            }
            set
            {
                this._baseUri = value;
            }
        }

        public override string Version
        {
            get
            {
                return this._version;
            }
            set
            {
                this._version = value;
            }
        }

        public override void InitTermMappings(RdfACoreParserContext context)
        {
            //TODO: Load RDFa default profile and HTML+RDFa profile
        }

        public override IEventGenerator<IRdfAEvent> GetEventGenerator(TextReader reader)
        {
            return new HtmlDomEventGenerator(reader, this);
        }

        public override void ParsePrefixMappings(RdfACoreParserContext context, IRdfAEvent evt)
        {
            foreach (KeyValuePair<String, String> attr in evt.Attributes)
            {
                if (attr.Key.Equals("xmlns"))
                {
                    context.Namespaces.AddNamespace(String.Empty, new Uri(Tools.ResolveUri(attr.Value, context.BaseUri.ToSafeString())));
                }
                else if (attr.Key.StartsWith("xmlns:"))
                {
                    String prefix = attr.Key.Substring(attr.Key.IndexOf(':') + 1);
                    context.Namespaces.AddNamespace(prefix, new Uri(Tools.ResolveUri(attr.Value, context.BaseUri.ToSafeString())));
                }
            }
        }

        public override void ParseLiteralLanguage(RdfACoreParserContext context, IRdfAEvent evt)
        {
            if (evt.HasAttribute("xml:lang"))
            {
                if (RdfSpecsHelper.IsValidLangSpecifier(evt["xml:lang"]))
                {
                    context.Language = evt["xml:lang"];
                }
            }
            else if (evt.HasAttribute("lang"))
            {
                if (RdfSpecsHelper.IsValidLangSpecifier(evt["lang"]))
                {
                    context.Language = evt["lang"];
                }
            }
        }

        public override bool IsRootElement(IRdfAEvent evt)
        {
            if (evt.EventType == Event.Element)
            {
                String name = ((ElementEvent)evt).Name;
                return name.Equals("head") || name.Equals("body");
            }
            else
            {
                return false;
            }
        }
    }

    public class HtmlHost : XHtmlHost
    {

    }
}

#endif