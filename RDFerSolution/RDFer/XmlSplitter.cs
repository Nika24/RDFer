using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using System.Xml.XPath;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    public enum ESplitType {Filesize,ElementCount};

    public class XmlSplitter
    {
        private XmlTextReader _data;
        private XmlTextWriter _output;
        private StringBuilder _outputBuilder;
        private string _outputFolder;
        private string _dataPath;

        public int SplitTotal;

        public XmlSplitter(string dataPath, string outputFolder) 
        {
            _outputBuilder = new StringBuilder();
            _data = new XmlTextReader(dataPath);
            _dataPath = dataPath;
            _output = new XmlTextWriter(new StringWriter(_outputBuilder));
            _outputFolder = outputFolder;
        }

        public void Split(ESplitType splitType, int splitSize,string fileSuffixXpath) 
        {

            _data.Read();

            //Capture all of the items before the first element
            while (_data.NodeType != XmlNodeType.Element) 
            {
                _output.WriteNode(_data, true);
            }

            //Prepare the document header and footer sections for use later...
            _output.WriteStartElement(_data.Name);
            _output.WriteAttributes(_data, true);
            var header = _outputBuilder.ToString() + ">";
            var footer = "</" + _data.Name + ">";

            //skip past the first node
            _data.Read();

            int elementCounter = 0;
            int splitCount = 0;

            bool emptyDocument = false;

            FileInfo dataFI = new FileInfo(_dataPath);
            string outputFileName = dataFI.Name.Substring(0,dataFI.Name.LastIndexOf("."));
            
            StreamWriter writer = null;
            while (!_data.EOF) 
            {
                //Only count the nodes that interest us...
                if (!IgnorableNodeType(_data.NodeType))
                    elementCounter++;
                else
                    emptyDocument = false;

                //copy everything from the reader
                _output.WriteNode(_data, true);

                if ((splitType == ESplitType.ElementCount && elementCounter >= splitSize) || (splitType == ESplitType.Filesize && (_outputBuilder.Length - header.Length) >= splitSize*1000))
                {
                    ////construct the final XML string
                    ExportXmlFile(footer, writer, outputFileName, splitCount);

                    //reset counters
                    splitCount++;
                    elementCounter = 0;
                    //reset string builder
                    _outputBuilder.Length = 0;
                    _outputBuilder.Append(header);
                    emptyDocument = true;
                }
            }

            //If there is anything left, export it - no footer required!
            if(_outputBuilder.Length > 0)
                ExportXmlFile("", writer, outputFileName, splitCount);

            SplitTotal = splitCount;
        }

        private void ExportXmlFile(string footer, StreamWriter writer, string outputFileName, int splitCount)
        {
            //construct the final XML string
            if(!string.IsNullOrEmpty(footer))
            {
                if (!_outputBuilder.ToString().EndsWith(footer))
                    _outputBuilder.Append(footer);
            }

            string splitFileName = Path.Combine(_outputFolder, outputFileName + "_" + splitCount.ToString() + ".xml");

            //output
            writer = new StreamWriter(splitFileName);
            writer.Write(_outputBuilder.ToString());
            writer.Close();
        }

        private List<string> ExecXpath(string attr, string xPathExpression, XmlDocument xmlDocument)
        {
            List<string> toReturn = new List<string>();

            var xpaths = Regex.Matches(xPathExpression, "{(.*?)}");
            if (xPathExpression.Contains("{") && xpaths.Count > 0)
            {
                //  if there is more than one xpath to look up in this value,
                //  object-{obj_id}-{part_id} then we want only one match for
                //  each xpath term
                if (xpaths.Count > 1 || xPathExpression.Substring(0, 1) != "{" || xPathExpression.Substring(xPathExpression.Length - 1, 1) != "}")
                {
                    bool matchedSomething = false;
                    foreach (Match xPath in xpaths)
                    {
                        string result;

                        XPathNavigator nav = xmlDocument.CreateNavigator();
                        XPathExpression expr = nav.Compile(RemoveClosingBrackets(xPath.Value));

                        switch (expr.ReturnType)
                        {
                            case XPathResultType.NodeSet:
                                XPathNodeIterator iterator = (XPathNodeIterator)nav.Select(expr);
                                if (iterator.Count == 1 || (xpaths.Count == 1 && iterator.Count > 0))
                                {
                                    iterator.MoveNext();

                                    if (iterator.Current.Value.Trim().Length > 0)
                                        matchedSomething = true;
                                    xPathExpression = xPathExpression.Replace(xPath.Value, iterator.Current.Value);
                                }
                                else if (xpaths.Count != 1 && iterator.Count == 0)
                                {
                                    throw new Exception("No match for " + xPath.Value + " in " + attr + "=\"" +
                                                        xPathExpression + "\"");
                                }
                                else if (xpaths.Count > 1)
                                {
                                    throw new Exception("Multiple matches for " + xPath.Value + " in " + attr +
                                                        "=\"" + xPathExpression + "\"");
                                }
                                break;
                            case XPathResultType.String:
                                string st = (string)nav.Evaluate(expr);
                                if (!string.IsNullOrEmpty(st) && st.Trim().Length > 0)
                                {
                                    matchedSomething = true;
                                    xPathExpression = xPathExpression.Replace(xPath.Value, st);
                                }
                                break;
                            case XPathResultType.Number:
                                //Assume it's a double
                                double dNumber = (double)nav.Evaluate(expr);
                                int iNumber;
                                matchedSomething = true;
                                //if it can be an int, then make it so...
                                if (int.TryParse(dNumber.ToString(), out iNumber))
                                {
                                    xPathExpression = xPathExpression.Replace(xPath.Value, iNumber.ToString());
                                }
                                else
                                {
                                    xPathExpression = xPathExpression.Replace(xPath.Value, dNumber.ToString());
                                }
                                break;
                            default:
                                throw new Exception("No match for " + xPath.Value + " in " + attr + "=\"" + xPathExpression +
                                                    "\"");
                        }
                        

                    }

                    if (matchedSomething && !string.IsNullOrEmpty(xPathExpression.Trim()))
                        toReturn.Add(xPathExpression);

                }
                else
                {

                    //  quite happy to enumerate for all values which match the xpath
                    var xpath = RemoveClosingBrackets(xPathExpression);
                    string result;
                    
                    var matches = xmlDocument.SelectNodes(xpath);
                    foreach (XmlNode node in matches)
                    {
                        //  ignore empty strings
                        if (!string.IsNullOrEmpty(node.InnerText))
                        {
                            //  apply modification function if required
                            var v = xPathExpression.Replace("{" + xpath + "}", node.InnerText);
                            if (!string.IsNullOrEmpty(v.Trim()))
                                toReturn.Add(v);
                        }
                    }
                }
            }
            return toReturn;
        }
        
        private static string RemoveClosingBrackets(string match)
        {
            var matchNew = match;
            if (matchNew.StartsWith("{"))
                matchNew = matchNew.Remove(0, 1);
            if (matchNew.EndsWith("}"))
                matchNew = matchNew.Remove(matchNew.Length - 1, 1);
            return matchNew;
        }
        private bool IgnorableNodeType(XmlNodeType nodeType)
        {
            return (nodeType == XmlNodeType.Whitespace ||
                nodeType == XmlNodeType.SignificantWhitespace ||
                nodeType == XmlNodeType.EndEntity ||
                nodeType == XmlNodeType.EndElement);
       }
    }
}
