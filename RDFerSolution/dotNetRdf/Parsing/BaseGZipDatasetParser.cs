﻿/*

Copyright Robert Vesse 2009-12
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

#if !NO_COMPRESSION

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Storage.Params;

namespace VDS.RDF.Parsing
{
    /// <summary>
    /// Abstract Base Class for parsers that handle GZipped input
    /// </summary>
    /// <remarks>
    /// <para>
    /// While the normal parsers can be used with GZip streams directly this class just abstracts the wrapping of file/stream input into a GZip stream if it is not already passed as such
    /// </para>
    /// </remarks>
    public abstract class BaseGZipDatasetParser
        : IStoreReader
    {
        private IStoreReader _parser;

        /// <summary>
        /// Creates a new GZipped input parser
        /// </summary>
        /// <param name="parser">The underlying parser to use</param>
        public BaseGZipDatasetParser(IStoreReader parser)
        {
            if (parser == null) throw new ArgumentNullException("parser");
            this._parser = parser;
            this._parser.Warning += this.RaiseWarning;
        }

        /// <summary>
        /// Loads a RDF dataset from GZipped input
        /// </summary>
        /// <param name="store">Triple Store to load into</param>
        /// <param name="parameters">Store Parameters</param>
        public void Load(ITripleStore store, IStoreParams parameters)
        {
            if (store == null) throw new RdfParseException("Cannot parse an RDF Dataset into a null store");
            this.Load(new StoreHandler(store), parameters);
        }

        /// <summary>
        /// Loads a RDF dataset from GZipped input
        /// </summary>
        /// <param name="handler">RDF Handler to use</param>
        /// <param name="parameters">Store Parameters</param>
        public void Load(IRdfHandler handler, IStoreParams parameters)
        {
            if (handler == null) throw new RdfParseException("Cannot parse RDF Dataset using a null Handler");
            if (parameters == null) throw new RdfParseException("Cannot parse RDF Dataset from null parameters");

            if (parameters is StreamParams)
            {
                StreamParams sp = (StreamParams)parameters;
                StreamReader input = sp.StreamReader;

                if (input.BaseStream is GZipStream)
                {
                    this._parser.Load(handler, sp);
                }
                else
                {
                    //Force the inner stream to be GZipped
                    this._parser.Load(handler, new StreamParams(new GZipStream(input.BaseStream, CompressionMode.Decompress)));
                }
            }
            else
            {
                throw new RdfParseException("GZip Dataset Parsers can only read from StreamParams instances");
            }
        }

        /// <summary>
        /// Warning Event raised on non-fatal errors encountered parsing
        /// </summary>
        public event StoreReaderWarning Warning;

        /// <summary>
        /// Helper method for raising warning events
        /// </summary>
        /// <param name="message">Warning Message</param>
        private void RaiseWarning(String message)
        {
            StoreReaderWarning d = this.Warning;
            if (d != null) d(message);
        }

        /// <summary>
        /// Gets the description of the parser
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "GZipped " + this._parser.ToString();
        }
    }

    /// <summary>
    /// Parser for loading GZipped NQuads
    /// </summary>
    public class GZippedNQuadsParser
        : BaseGZipDatasetParser
    {
        /// <summary>
        /// Creates a new GZipped NQuads Parser
        /// </summary>
        public GZippedNQuadsParser()
            : base(new NQuadsParser()) { }
    }

    /// <summary>
    /// Parser for loading GZipped TriG
    /// </summary>
    public class GZippedTriGParser
        : BaseGZipDatasetParser
    {
        /// <summary>
        /// Creates a new GZipped TriG Parser
        /// </summary>
        public GZippedTriGParser()
            : base(new TriGParser()) { }
    }

    /// <summary>
    /// Parser for loading GZipped TriX
    /// </summary>
    public class GZippedTriXParser
        : BaseGZipDatasetParser
    {
        /// <summary>
        /// Creates a new GZipped TriX Parser
        /// </summary>
        public GZippedTriXParser()
            : base(new TriXParser()) { }
    }
}

#endif