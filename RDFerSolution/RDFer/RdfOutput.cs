using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Xml;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Storage.Params;
using VDS.RDF.Writing;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    public enum ELiteralType 
    {
        XsdInteger,
        XsdBoolean,
        XsdDateTime
    }
    public enum ERdfFormat
    {
        RdfXml,
        TriG,
        Turtle,
        NTriples,
        N3,
        NQuads
    }
    public class RdfOutput : IDisposable
    {
        private XmlDocument _output;
        private string _outputPath;
        private Dictionary<string, string> _namespaces;
        private ERdfFormat _outputFormat;

        //If TRIG formate is used, then this will hold all named graphs
        private TripleStore _store;
        private RdfXmlParser _xmlParser;

        public XmlNode RdfRootNode { get; set; } 

        public RdfOutput(string outputPath, Dictionary<string,string> namespaces)
        {
            _outputPath = outputPath;
            InitialiseRdfOutput(namespaces);
            _outputFormat = ERdfFormat.RdfXml;
        }
        public RdfOutput(string outputPath, Dictionary<string, string> namespaces,ERdfFormat outputFormat)
        {
            _outputPath = outputPath;
            InitialiseRdfOutput(namespaces);
            _outputFormat = outputFormat;
        }
        public void InitialiseRdfOutput(Dictionary<string, string> namespaces)
        {
            _output = new XmlDocument();

            _namespaces = namespaces;

            XmlDeclaration xmlDec = _output.CreateXmlDeclaration("1.0", "utf-8", null);
            XmlElement root = _output.DocumentElement;
            _output.InsertBefore(xmlDec, root);

            //add all of the entities
            string entities = string.Empty;
            foreach (string prefix in _namespaces.Keys)
            {
                var uri = _namespaces[prefix];
                entities += "  <!ENTITY " + prefix + " \"" + uri + "\">\n";
            }
            _output.AppendChild(_output.CreateDocumentType("rdfs", null, null, entities));

            RdfRootNode = _output.CreateElement("rdf", "RDF", _namespaces["rdf"]);

            //set up namespaces
            foreach (string prefix in _namespaces.Keys)
            {
                var uri = _namespaces[prefix];
                ((XmlElement)RdfRootNode).SetAttribute("xmlns:" + prefix, uri);
            }

            _output.AppendChild(RdfRootNode);
        }

        public void SetNamespaces(Dictionary<string, string> namespaces) 
        {
            _namespaces = namespaces;
        }

        public void AddIdentifierForResource(XmlNode resourceNode, string prefix, string uri)
        {
            XmlAttribute attribute = _output.CreateAttribute("rdf", "about", _namespaces["rdf"]);
            var entityReference = _output.CreateEntityReference(prefix);
            attribute.AppendChild(entityReference);
            attribute.Value += uri;
            resourceNode.Attributes.Append(attribute);
        }

        public XmlNode AddResourceWithoutIdentifier()
        {
            XmlNode node = _output.CreateElement("rdf", "Description", _namespaces["rdf"]);
            RdfRootNode.AppendChild(node);
            return node;
        }

        public XmlNode AddResourceWithIdentifier(string prefix, string uri)
        {
            XmlNode node = _output.CreateElement("rdf", "Description", _namespaces["rdf"]);
            XmlAttribute attribute = _output.CreateAttribute("rdf", "about", _namespaces["rdf"]);

            var entityReference = _output.CreateEntityReference(prefix);
            attribute.AppendChild(entityReference);
            attribute.Value += uri;
            node.Attributes.Append(attribute);
            RdfRootNode.AppendChild(node);
            return node;
        }
        public XmlNode AddResource()
        {
            XmlNode node = _output.CreateNode(XmlNodeType.Element, "rdf", "Description", _namespaces["rdf"]);
            RdfRootNode.AppendChild(node);
            return node;
        }
        public void AddPredicateAndObject(XmlNode subjectNode, string predicatePrefix, string predicateValue, string objectClassPrefix, string objectClassUri, string objectPrefix, string objectUri)
        {
            XmlElement node = _output.CreateElement(predicatePrefix, predicateValue, _namespaces[predicatePrefix]);

            XmlAttribute attribute = _output.CreateAttribute(objectClassPrefix, objectClassUri,
                                                             _namespaces[objectClassPrefix]);

            if(string.IsNullOrEmpty(objectPrefix))
            {
                attribute.Value = objectUri;
            }
            else
            {
                var entityReference = _output.CreateEntityReference(objectPrefix);
                attribute.AppendChild(entityReference);
                attribute.Value += objectUri;
            }
            node.Attributes.Append(attribute);
            subjectNode.AppendChild(node);
        }
        public void AddPredicateAndLiteral(XmlNode subjectNode, string predicatePrefix, string predicateValue, string literal)
        {
            XmlNode node = _output.CreateElement(predicatePrefix + ":" + predicateValue, _namespaces[predicatePrefix]);
            node.InnerText = literal;
            subjectNode.AppendChild(node);
        }
        public void AddPredicateAndLiteralWithType(XmlNode subjectNode, string predicatePrefix, string predicateValue, string literal, string typeUri)
        {
            XmlNode node = _output.CreateNode(XmlNodeType.Element, predicatePrefix, predicateValue, _namespaces[predicatePrefix]);
            XmlAttribute attribute = null;
            if (!string.IsNullOrEmpty(typeUri))
            {
                attribute = _output.CreateAttribute("rdf", "datatype", _namespaces["rdf"]);
                attribute.Value = typeUri;
                node.Attributes.Append(attribute);
                node.InnerText = literal;
            }
            subjectNode.AppendChild(node);
        }
        public void AddPredicateAndLiteralWithLanguage(XmlNode subjectNode, string predicatePrefix, string predicateValue, string literal, string languageCode)
        {
            XmlNode node = _output.CreateNode(XmlNodeType.Element, predicatePrefix, predicateValue, _namespaces[predicatePrefix]);
            XmlAttribute attribute = null;
            if (!string.IsNullOrEmpty(languageCode))
            {
                attribute = _output.CreateAttribute("xml:lang");
                attribute.Value = languageCode;
                node.Attributes.Append(attribute);
                node.InnerText = literal;
            }
            subjectNode.AppendChild(node);
        }

        public void AddPredicateAndLiteralWithType(XmlNode subjectNode, string predicatePrefix, string predicateValue, string literal, ELiteralType literalType)
        {
            XmlNode node = _output.CreateNode(XmlNodeType.Element, predicatePrefix, predicateValue, _namespaces[predicatePrefix]);
            XmlAttribute attribute = null;
            switch (literalType)
            {
                case ELiteralType.XsdInteger:
                    attribute = _output.CreateAttribute("xsd", "integer", _namespaces["xsd"]);
                    break;
                case ELiteralType.XsdBoolean:
                    attribute = _output.CreateAttribute("xsd", "boolean", _namespaces["xsd"]);
                    break;
                case ELiteralType.XsdDateTime:
                    attribute = _output.CreateAttribute("xsd", "dateTime", _namespaces["xsd"]);
                    break;
            }
            if(attribute != null)
            {
                attribute.Value = literal;
                node.Attributes.Append(attribute);
            }
            subjectNode.AppendChild(node);
        }

        public XmlNode AddBnode(string bNodeName)
        {
            var bNodeNameParts = bNodeName.Split(':');

            if (bNodeNameParts.Length != 2)
                throw new Exception("Blank Node must be in form: 'ns:Type'");

            return _output.CreateElement(bNodeNameParts[0] + ":" + bNodeNameParts[1],_namespaces[bNodeNameParts[0]]);
        }

        public void AddNamedGraphToCurrentNodes(string namedGraphUri)
        {
            //as we're using named graphs now, we must use TRIG format
            if (this._outputFormat != ERdfFormat.TriG && this._outputFormat != ERdfFormat.NQuads)
            {
                this._outputFormat = ERdfFormat.TriG;
                Console.WriteLine("Format set to TriG as namedgraph has been asserted.");
            }

            //Load the nodes into a graph, and name it
            var g = GetXmlDocumentAsGraph();
            g.BaseUri = new Uri(namedGraphUri);

            //add to triple store
            if (_store == null)
                _store = new TripleStore();
            _store.Add(g, true);

            //empty the current XmlDocument and restart
            InitialiseRdfOutput(_namespaces);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this._outputFormat == ERdfFormat.RdfXml)
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(_outputPath, new UTF8Encoding(false))) //Set encoding
                {
                    _output.Save(xmlWriter);
                }
            }
            else if (this._outputFormat == ERdfFormat.TriG)
            {
                string fileNameAsTrig = GetFilePathBasedOnFormat();
                var outparams = new StreamParams(fileNameAsTrig);
                outparams.Encoding = Encoding.UTF8;
                var writer = new TriGWriter();

                if (_store == null)
                {
                    var g = GetXmlDocumentAsGraph();
                    _store = new TripleStore();
                    _store.Add(g, true);
                }

                writer.Save(_store, outparams);
            }
            else if (this._outputFormat == ERdfFormat.Turtle)
            {
                var g = GetXmlDocumentAsGraph();
                string filePathForFormat = GetFilePathBasedOnFormat();
                var writer = new TurtleWriter(TurtleSyntax.W3C);
                writer.Save(g, filePathForFormat);
            }
            else if (this._outputFormat == ERdfFormat.NTriples)
            {
                var g = GetXmlDocumentAsGraph();
                string filePathForFormat = GetFilePathBasedOnFormat();
                var writer = new NTriplesWriter();
                writer.Save(g, filePathForFormat);
            }
            else if (this._outputFormat == ERdfFormat.N3)
            {
                var g = GetXmlDocumentAsGraph();
                string filePathForFormat = GetFilePathBasedOnFormat();
                var writer = new Notation3Writer();
                writer.Save(g, filePathForFormat);
            }
            else if (this._outputFormat == ERdfFormat.NQuads)
            {
                string filePathForFormat = GetFilePathBasedOnFormat();
                var outparams = new StreamParams(filePathForFormat);
                outparams.Encoding = Encoding.UTF8;

                if (_store == null)
                {
                    var g = GetXmlDocumentAsGraph();
                    _store = new TripleStore();
                    _store.Add(g, true);
                }
                
                var writer = new NQuadsWriter();
                writer.Save(_store, outparams);
            }

            //make sure it's not null - can happen if no graphs have yet to be asserted!!
            if (_store != null)
            {
                foreach (var graph in _store.Graphs)
                {
                    graph.Dispose();
                }
                _store.Dispose();
                GC.Collect();
            }
        }

        #endregion

        #region Private Methods
        private Graph GetXmlDocumentAsGraph()
        {
            var g = new Graph();

            if (_xmlParser == null)
            {
                _xmlParser = new RdfXmlParser(RdfXmlParserMode.DOM);
                //This is not to store the URIs in a static list by dotNetRDF (uses up a lot of mem)
                Options.InternUris = false;
            }

            _xmlParser.Load(g, _output);

            //add namespaces from the XML doc to the graph
            foreach (var nsPrefix in _namespaces.Keys)
            {
                if (!g.NamespaceMap.HasNamespace(nsPrefix))
                    g.NamespaceMap.AddNamespace(nsPrefix, new Uri(_namespaces[nsPrefix]));
            }

            return g;
        }

        private string GetFilePathBasedOnFormat()
        {
            string extension = ".rdf";
            switch (_outputFormat)
            {
                case ERdfFormat.N3:
                    extension = ".n3";
                    break;
                case ERdfFormat.NTriples:
                    extension = ".nt";
                    break;
                case ERdfFormat.RdfXml:
                    extension = ".rdf";
                    break;
                case ERdfFormat.TriG:
                    extension = ".trig";
                    break;
                case ERdfFormat.Turtle:
                    extension = ".ttl";
                    break;
                case ERdfFormat.NQuads:
                    extension = ".nq";
                    break;
            }

            if (!_outputPath.ToLower().EndsWith(extension))
                return Path.Combine(new FileInfo(_outputPath).DirectoryName , Path.GetFileNameWithoutExtension(_outputPath) + extension);

            return _outputPath;
        }
        
        #endregion
    }
}
