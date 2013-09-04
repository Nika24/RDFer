using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;
using System.Xml.XPath;
using JoshanMahmud.SemanticWeb.ModifierLibrary;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    public class RDFer : IDisposable
    {
        private XmlDocument _data { get; set; }
        private XmlDocument _config { get; set; }
        private RdfOutput _output { get; set; }
        
        private Dictionary<string, string> _namespaces;
        private Dictionary<string, string> _months;

        private XmlNamespaceManager _namespaceManager;

        //other user parameters
        private string _outputPath;
        private string _dataPath;
        private bool _includeStackTraceInOutput;
        private string _format;

        //Global settings for elements (means only one thing can be used at a time
        private bool _ifStatementMatch = false;
        
        //A list of all counters
        private Dictionary<string,Counter> _counters = new Dictionary<string, Counter>();

        //A list of all unique identifers
        private Dictionary<string, UniqueIdentifier> _uniqueIdentifiers = new Dictionary<string, UniqueIdentifier>();

        public RDFer(string dataPath, string configPath, string outputPath, bool includeStackTraceInOutput, string format) 
        {
            //set defaults
            _months = GetMonths();

            //Set inputs
            _data = new XmlDocument();
            _data.Load(dataPath);
            _dataPath = dataPath;

            _namespaceManager = GetNamespacesFromData(_data, "vp");

            _config = new XmlDocument();
            _config.Load(configPath);

            //save output file
            _outputPath = outputPath;
            _includeStackTraceInOutput = includeStackTraceInOutput;
            _format = format;
        }

        public void Start() 
        {
            try
            {
                //initialise to set up defaults
                InitialiseNamespaces();

                //Get config namespaces
                GetNamespaces(_config.SelectSingleNode("config/namespaces"));

                //set namepaces and then we can initialise the output

                //the outputPath requires the name, so this will be obtained from the input filename:
                FileInfo inputFI = new FileInfo(_dataPath);
                string outputFileName = inputFI.Name.Substring(0, inputFI.Name.LastIndexOf("."));

                _output = new RdfOutput(Path.Combine(_outputPath, outputFileName + ".rdf"), _namespaces, ParseFormat(_format));

                //Process config
                XmlNodeList nodes = _config.GetElementsByTagName("config");

                foreach (XmlNode node in nodes)
                {
                    ExecConfig(null, node, _data);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in Start():" + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }
        private void InitialiseNamespaces() 
        {
            _namespaces = new Dictionary<string, string>();
            
            //add default namespaces
            _namespaces.Add("owl", "http://www.w3.org/2002/07/owl#");
            _namespaces.Add("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            _namespaces.Add("rdfs", "http://www.w3.org/2000/01/rdf-schema#");

        }
        private static ERdfFormat ParseFormat(string format)
        {
            ERdfFormat eRdfFormat = ERdfFormat.RdfXml;
            switch(format.Trim().ToLower())
            {
                case "rdf":
                    eRdfFormat = ERdfFormat.RdfXml;
                    break;
                case "ttl":
                case "turtle":
                    eRdfFormat = ERdfFormat.Turtle;
                    break;
                case "n3":
                case "notation3":
                    eRdfFormat = ERdfFormat.N3;
                    break;
                case "trig":
                    eRdfFormat = ERdfFormat.TriG;
                    break;
                case "nt":
                case "ntriples":
                    eRdfFormat = ERdfFormat.NTriples;
                    break;
            }
            return eRdfFormat;
        }

        private void ExecConfig(ResourceNode resourceNode, XmlNode config, XmlNode data) 
        {
            try
            {
                foreach (XmlNode node in config.ChildNodes)
                {
                    string elementFunc = node.Name.Substring(0, 1).ToUpper() + node.Name.Substring(1).ToLower();

                    string func = "Do" + elementFunc;

                    object[] parameters = new object[3] {resourceNode, node, data};
                    switch (func)
                    {
                            //ignore these ones
                        case "Do#text":
                        case "Do#comment":
                        case "DoPartition":
                        case "DoNamespaces":
                        case "DoSplit":
                        case "DoNamedmapping":
                            continue;
                        case "DoForeach":
                            func = "DoMapping";
                            break;
                        case "DoResource":
                            parameters = new object[2] {node, data};
                            break;
                        case "DoIdentifier":
                            //check to see if we have already found the identifier for the current resource - if not, then process
                            if (resourceNode.HasIdentifier)
                                continue;
                            break;
                    }

                    Type thisType = this.GetType();
                    MethodInfo theMethod = thisType.GetMethod(func, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (theMethod != null)
                    {
                        theMethod.Invoke(this, parameters);
                    }
                    else
                        throw new Exception("Cannot process: " + node.OuterXml + ". Cannot identify tag: <" + elementFunc +">"); 
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in ExecConfig(): " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
                if (ex.InnerException != null)
                    Console.WriteLine(ex.InnerException.Message + (_includeStackTraceInOutput ? ex.InnerException.StackTrace : ""));
            }
        }
        private void GetNamespaces(XmlNode namespaces) 
        {
            try
            {
                if (namespaces == null || _namespaces == null)
                    throw new Exception("Error in config: <namespaces> has not been defined");

                //loop through all of the namespaces
                foreach (XmlNode namespaceNode in namespaces.ChildNodes)
                {
                    //check attributes are there
                    if (namespaceNode.Attributes["prefix"] == null || namespaceNode.Attributes["uri"] == null)
                        throw new Exception("Error in config: Missing prefix or uri attribute in <namespace> element");

                    //get prefix
                    var prefix = namespaceNode.Attributes["prefix"].Value;
                    //get uri
                    var uri = namespaceNode.Attributes["uri"].Value;

                    if (!_namespaces.ContainsKey(prefix))
                        _namespaces.Add(prefix, uri);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in GetNamespaces():" + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }

        }
        private void DoError(ResourceNode resourceNode, XmlNode errorNode, XmlNode data) 
        {
            string message = string.Empty;
            bool exit = false;

            if (errorNode.Attributes["message"] == null)
                throw new Exception("Error in config: Missing attribute @message in <error> element.  Please specify the message for the error.");
            else
                message = errorNode.Attributes["message"].Value.ToString();

            if(errorNode.Attributes["exit"] != null)
                if(!bool.TryParse(errorNode.Attributes["exit"].Value.ToString(),out exit))
                    throw new Exception("Error in config: Unknown value for attribute @exit in <error> element.  Please specify either true or false.");

            //if user wishes to end the application then throw exception
            //else just log it to the console.
            if (exit)
                throw new Exception("User specified error has occured at: " + resourceNode.xmlResouceNode.InnerXml + ",\nDue to error statement in config: " + message);
            else
                Console.WriteLine("User specified error has occured at: " + resourceNode.xmlResouceNode.InnerXml + ",\nDue to error statement in config: " + message);
        }
        private void DoCounter(ResourceNode resourceNode, XmlNode counterNode, XmlNode data)
        {
            //default initial value of counter & iteration
            int initialValue = 0;
            eCounterIteration iteration = eCounterIteration.Increment;
            string counterName = string.Empty;
            bool iterate = false;

            if (counterNode.Attributes["name"] == null)
                throw new Exception("Error in config: Missing attribut @name in <counter> element.  Please specify a name of the counter variable.");
            counterName = counterNode.Attributes["name"].Value;
            
            if (counterNode.Attributes["iteration"] != null)
            {
                if (counterNode.Attributes["iteration"].Value.Trim().ToLower() == "decrement")
                {
                    iteration = eCounterIteration.Decrement;
                }
            }

            if (counterNode.Attributes["initialValue"] != null)
            {
                if (!int.TryParse(counterNode.Attributes["initialValue"].Value, out initialValue))
                {
                    throw new Exception("Error in config: @initialValue attribute in <counter> element is not a number/integer.");
                }
            }

            if (counterNode.Attributes["iterate"] != null)
            {
                if (!bool.TryParse(counterNode.Attributes["iterate"].Value, out iterate))
                {
                    throw new Exception("Error in config: @iterate attribute in <counter> element is not a true/false");
                }
            }

            if (iterate)
            {
                var counter = _counters[counterName];
                counter.IterateCounter();
            }
            else
            {
                //check if it is in there first...
                Counter counter = null;
                if (_counters.ContainsKey(counterName))
                    counter = _counters[counterName];
                else
                    //if we get here we'll create a new counter
                    counter = new Counter(counterName);

                counter.CounterValue = initialValue;
                counter.Iteration = iteration;

                try
                {
                    if (!_counters.ContainsKey(counterName))
                        _counters.Add(counterName, counter);
                }
                catch (Exception ex)
                {
                    throw new Exception("An issue occured when storing the counter variable: " + ex.Message);
                }
            }
        }
        private void DoUniqueidentifier(ResourceNode resourceNode, XmlNode uniqueidentifierNode, XmlNode data)
        {
            string uniqueIdentifierName = string.Empty;
            bool generate = true;

            //there must be a name and generate must equal true.
            if (uniqueidentifierNode.Attributes["name"] == null)
                throw new Exception("Error in config: Missing attribut @name in <uniqueidentifier> element.  Please specify a name of the unique identifier variable.");
            uniqueIdentifierName = uniqueidentifierNode.Attributes["name"].Value;
            if (uniqueidentifierNode.Attributes["generate"] != null)
            {
                if(!bool.TryParse(uniqueidentifierNode.Attributes["generate"].Value,out generate))
                {
                    throw new Exception("Error in config: @generate attribute in <uniqueidentifier> element is not a true/false");
                }
            }

            if (generate)
            {
                //first check to see if the identifier already exists - if not, then create and add it!
                UniqueIdentifier uniqueIdentifier = null;
                if (_uniqueIdentifiers.ContainsKey(uniqueIdentifierName))
                    uniqueIdentifier = _uniqueIdentifiers[uniqueIdentifierName];
                else
                {
                    uniqueIdentifier = new UniqueIdentifier(uniqueIdentifierName);
                    _uniqueIdentifiers.Add(uniqueIdentifierName, uniqueIdentifier);
                }
                
                //Do the generation
                uniqueIdentifier.Generate();
            }
        }

        private bool TryInternalFunction(string funcCall, out string result)
        {
            //it is a counter 
            result = string.Empty;
            if(funcCall.StartsWith("counter_"))
            {
                //get name of counter
                var counterName = funcCall.Substring(8);

                if(!_counters.ContainsKey(counterName))
                    throw new Exception("No counter named:" + counterName + "exists and can be used.");

                var counter = _counters[counterName];
                result = counter.CounterValue.ToString();
                return true;
            }
            else if(funcCall.StartsWith("uniqueidentifier_"))
            {
                var uniqueIdentifierName = funcCall.Substring(17);

                if(!_uniqueIdentifiers.ContainsKey(uniqueIdentifierName))
                    throw new Exception("No unique identifier named:" + uniqueIdentifierName + "exists and can be used.");

                var uniqueIdentifer = _uniqueIdentifiers[uniqueIdentifierName];
                result = uniqueIdentifer.UniqueId;
                return true;
            }
            return false;
        }

        private void DoIdentifier(ResourceNode resourceNode, XmlNode identifier, XmlNode data)
        {
            //We have an identifier!
            resourceNode.HasIdentifier = true;

            ModifierDelegate modifierDelegate = null;
            if (identifier.Attributes["modifier"] != null)
                modifierDelegate = Modifier.GetModifierMethod(identifier.Attributes["modifier"].Value);

            //  grab the value of the required field from the data
            string[] ids = GetValues("value", identifier, data, modifierDelegate, 1, 1);
            foreach (string idValue in ids)
            {
                //  apply modifier to the value?
                //  finally output the identifier as per the spec

                var id = SanitiseUri(idValue);
                string prefix = string.Empty;
                string shortUri = string.Empty;

                //convert to short url
                ShortUri(id, out prefix, out shortUri);

                _output.AddIdentifierForResource(resourceNode.xmlResouceNode, prefix, shortUri);
            }
        }

        private void DoUsenamedmapping(ResourceNode resourceNode, XmlNode mapping, XmlNode data)
        {
            //check if there is a specified name attribute
            if(mapping.Attributes["name"] == null)
                throw new Exception("Error in config: Missing attribut @name in <usenamedmapping> element.  Please specify a name of a namedmapping.");

            var namedMappingName = mapping.Attributes["name"];
            var namedMapping = _config.SelectSingleNode("//config/namedmapping[@name='" + namedMappingName.Value + "']");

            if(namedMapping == null)
                throw new Exception("Error in config: no named mapping was found for: " + namedMappingName + ".  Please ensure that a <namedmapping> element is used and defined directly under <config>");

            //recurse
            ExecConfig(resourceNode, namedMapping, data);
        }

        private void DoMapping(ResourceNode resourceNode, XmlNode mapping, XmlNode data) 
        {
            try
            {
                //TODO: set xpath globally for error output
                if (mapping.Attributes["match"] == null)
                    throw new Exception("Error in config: Missing match=XPATH attribute in <mapping>");

                //get xpath without the closing {} 
                var match = RemoveClosingBrackets(mapping.Attributes["match"].Value);
                match = MatchNestedInternalFunctionCalls(match);

                //execute xpath on data
                var nodeList = data.SelectNodes(match, _namespaceManager);

                foreach (XmlNode node in nodeList)
                {
                    ExecConfig(resourceNode, mapping, node);

                    try
                    {
                        /*get the named graph URI*/
                        if (mapping.Attributes["namedgraph"] != null)
                        {
                            string[] namedGraphs = GetValues("namedgraph", mapping, node);
                            if (namedGraphs.Length == 1)
                            {
                                _output.AddNamedGraphToCurrentNodes(SanitiseUri(namedGraphs[0]));
                            }
                            else
                            {
                                Console.WriteLine(
                                    "Error in Mapping tag: too many values found in attribute: @namedgraph.  No named graph asserted.");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Error in Mapping tag with namedgraph attribute: " + ex.Message);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in Mapping tag: " + mapping.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }

        private void DoResource(XmlNode resourceMapping, XmlNode data) 
        {
            try
            {
                
                //XmlNode resourceNode = null;
                ResourceNode resourceNode = new ResourceNode();

                //deal with the identifier (if present)
                XmlNode[] identifiers = GetChildren(resourceMapping, "identifier");

                //if (identifiers.Count<XmlNode>() > 1)
                //    throw new Exception("Error in config: A <resource> may only have one <identifier>");

                if (identifiers.Count<XmlNode>() == 1)
                {
                    //We have an identifier!
                    resourceNode.HasIdentifier = true;

                    //  we have a valid identifier specification
                    XmlNode identifier = identifiers[0];

                    ModifierDelegate modifierDelegate = null;
                    if (identifier.Attributes["modifier"] != null)
                        modifierDelegate = Modifier.GetModifierMethod(identifier.Attributes["modifier"].Value);

                    //  grab the value of the required field from the data
                    string[] ids = GetValues("value", identifier, data, modifierDelegate, 1, 1);
                    foreach (string idValue in ids)
                    {
                        //  apply modifier to the value?
                        //  finally output the identifier as per the spec

                        var id = SanitiseUri(idValue);
                        string prefix = string.Empty;
                        string shortUri = string.Empty;

                        //convert to short url
                        ShortUri(id, out prefix, out shortUri);

                        resourceNode.xmlResouceNode = _output.AddResourceWithIdentifier(prefix, shortUri);

                    }
                }
                else
                {
                    //  no identifier, outout a bnode. b-node
                    resourceNode.xmlResouceNode = _output.AddResource();
                }

                //recurse
                ExecConfig(resourceNode, resourceMapping, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Resource tag: " + resourceMapping.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }
        private void DoIf(ResourceNode resourceNode, XmlNode ifNode, XmlNode data) 
        {
            try
            {
                //validate
                if (ifNode.Attributes["match"] == null)
                    throw new Exception("Error in config: Missing match=\"...\" attribute in <if>\n\n");

                var match = RemoveClosingBrackets(ifNode.Attributes["match"].Value);
                match = MatchNestedInternalFunctionCalls(match);

                XPathNavigator nav = data.CreateNavigator();
                XPathExpression expr = nav.Compile(RemoveClosingBrackets(match));
                expr.SetContext(_namespaceManager);

                _ifStatementMatch = false;
                switch (expr.ReturnType)
                {
                    case XPathResultType.NodeSet:
                        XPathNodeIterator iterator = (XPathNodeIterator)nav.Select(expr);
                        if (iterator.Count > 0)
                        {
                            ExecConfig(resourceNode, ifNode, data);
                            _ifStatementMatch = true;
                        }
                        break;
                    case XPathResultType.Boolean:
                        if((bool)nav.Evaluate(expr))
                        {
                            ExecConfig(resourceNode, ifNode, data);
                            _ifStatementMatch = true;
                        }
                        break;
                    case XPathResultType.String:
                        string st = (string)nav.Evaluate(expr);
                        if (!string.IsNullOrEmpty(st) && st.Trim().Length > 0)
                        {
                            bool resultStr = false;
                            if (bool.TryParse(st, out resultStr))
                            {
                                if (resultStr)
                                {
                                    ExecConfig(resourceNode, ifNode, data);
                                    _ifStatementMatch = true;
                                }
                            }
                        }
                        break;
                    case XPathResultType.Number:
                        //Assume it's a double
                        double dNumber = (double)nav.Evaluate(expr);
                        int iNumber;
                        bool resultNum = false;
                        //if it can be an int, then make it so...
                        if (bool.TryParse(dNumber.ToString(), out resultNum))
                        {
                            if (resultNum)
                            {
                                ExecConfig(resourceNode, ifNode, data);
                                _ifStatementMatch = true;
                            }
                        }
                        break;
                    default:
                        throw new Exception("Could not evaluate If statement: " + match);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in If tag: " + ifNode.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }
        private void DoElse(ResourceNode resourceNode, XmlNode elseNode, XmlNode data)
        {
            if(!_ifStatementMatch)
                ExecConfig(resourceNode, elseNode, data);
        }

        private void DoSwitch(ResourceNode resourceNode, XmlNode switchNode, XmlNode data) 
        {
            try
            {
                XmlNodeList switchValues = null;

                //  does this switch have a match value?
                if (switchNode.Attributes["match"] != null)
                {
                    var match = RemoveClosingBrackets(switchNode.Attributes["match"].Value);
                    match = MatchNestedInternalFunctionCalls(match);
                    switchValues = data.SelectNodes(match, _namespaceManager);
                }

                //  process cases, apply those which satisfy their
                //  match conditions
                bool caseMatched = false;
                var cases = GetChildren(switchNode, "case");
                foreach (XmlNode caseNode in cases)
                {
                    bool breakBoth = false;

                    if (caseNode.Attributes["match"] != null)
                    {
                        var match = RemoveClosingBrackets(caseNode.Attributes["match"].Value);
                        match = MatchNestedInternalFunctionCalls(match);

                        //  if that match attribute is satisfied...
                        var nodeList = data.SelectNodes(match, _namespaceManager);

                        if (nodeList.Count > 0)
                        {
                            caseMatched = true;
                            ExecConfig(resourceNode, caseNode, data);

                            //  by default when we find a matched case, we break and ignore
                            //  other cases, unless attribute break="false" is present
                            if (caseNode.Attributes["break"] == null)
                                break;
                        }


                    }
                    else if (caseNode.Attributes["value"] != null)
                    {
                        var value = caseNode.Attributes["value"].Value;
                        foreach (XmlNode v in switchValues)
                        {
                            if (v.InnerText == value)
                            {
                                caseMatched = true;
                                ExecConfig(resourceNode, caseNode, data);

                                //  by default when we find a matched case, we break and ignore
                                //  other cases, unless attribute break="false" is present
                                if (caseNode.Attributes["break"] != null)
                                {
                                    breakBoth = true;
                                    break;
                                }
                            }
                        }
                        if (breakBoth)
                            break;
                    }
                    else
                    {
                        throw new Exception(
                            "Error in config: <case> tag must have attribute match=\"{xpath}\" or value=\"...\"\n\n");
                    }
                }

                //  if no case statements have been matched and there is
                //  a default case then include anything from that
                if (!caseMatched)
                {
                    var defaults = GetChildren(switchNode, "default");
                    if (defaults.Length > 1)
                    {
                        throw new Exception("Error in config: <switch> tag must not contain more than one <default> tag");
                    }
                    if (defaults.Length == 1)
                    {
                        ExecConfig(resourceNode, defaults[0], data);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in Switch tag: " + switchNode.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }
        private void DoTriple(ResourceNode resourceNode, XmlNode triple, XmlNode data) 
        {
            try
            {
                //  if no predicate then abort!
                if (triple.Attributes["predicate"] == null)
                    throw new Exception("Error in config: Missing predicate=\"...\" attribute in <triple>");

                var pred = triple.Attributes["predicate"].Value;
                var predNs = pred.Split(':')[0];
                var predName = pred.Split(':')[1];

                ModifierDelegate mod_func = null;

                if (triple.Attributes["modifier"] != null)
                    mod_func = Modifier.GetModifierMethod(triple.Attributes["modifier"].Value);

                //  is it a literal or a resource?
                if (triple.Attributes["object"] != null)
                {
                    string[] objs = GetValues("object", triple, data, mod_func);
                    foreach (string obj in objs)
                    {
                        var obj2 = SanitiseUri(obj);
                        if (string.IsNullOrEmpty(obj2.Trim()))
                            continue;
                        string valuePrefix = string.Empty;
                        string valueUri = string.Empty;
                        ShortUri(obj2, out valuePrefix, out valueUri);

                        _output.AddPredicateAndObject(resourceNode.xmlResouceNode, predNs, predName, "rdf", "resource", valuePrefix,
                                                      valueUri);
                    }
                }
                else
                {
                    //literal
                    string[] literals = GetValues("value", triple, data, mod_func);
                    foreach (string lit in literals)
                    {
                        if (string.IsNullOrEmpty(lit))
                            continue;

                        var sanitisedLit = SanitiseString(lit.Trim());

                        //check to see if there is a type to assert
                        if (triple.Attributes["type"] != null)
                        {
                            var valueType = triple.Attributes["type"].Value;
                            _output.AddPredicateAndLiteralWithType(resourceNode.xmlResouceNode, predNs, predName, sanitisedLit,valueType);
                        }
                        else if(triple.Attributes["language"] != null)
                        {
                            var valueLang = triple.Attributes["language"].Value;
                            _output.AddPredicateAndLiteralWithLanguage(resourceNode.xmlResouceNode, predNs, predName, sanitisedLit, valueLang);
                        }
                        else
                        {
                            _output.AddPredicateAndLiteral(resourceNode.xmlResouceNode, predNs, predName, sanitisedLit);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in Triple tag: " + triple.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }

        }
        private void DoType(ResourceNode resourceNode, XmlNode type, XmlNode data) 
        {
            try
            {
                var types = GetValues("value", type, data, null, 1); //min=1
                foreach (string t in types)
                {
                    var prefix = string.Empty;
                    var uri = string.Empty;
                    ShortUri(t, out prefix, out uri);

                    _output.AddPredicateAndObject(resourceNode.xmlResouceNode, "rdf", "type", "rdf", "resource", prefix, uri);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in Type tag: " + type.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }

        private void DoBnode(ResourceNode resourceNode, XmlNode bNode, XmlNode data) 
        {
            try
            {
                if (bNode.Attributes["predicate"] == null)
                    throw new Exception("Error in config: Missing predicate=\"...\" attribute in <bnode>");
                if (bNode.Attributes["type"] == null)
                    throw new Exception("Error in config: Missing type=\"...\" attribute in <bnode>");

                var pred = bNode.Attributes["predicate"].Value;
                var type = bNode.Attributes["type"].Value;



                XmlNode predNode = _output.AddBnode(pred);
                ResourceNode typeResource = new ResourceNode();
                typeResource.xmlResouceNode = _output.AddBnode(type);

                predNode.AppendChild(typeResource.xmlResouceNode);
                resourceNode.xmlResouceNode.AppendChild(predNode);
                ExecConfig(typeResource, bNode, data);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in BNode tag: " + bNode.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }

        /*private void DoDate(ResourceNode resourceNode, XmlNode date, XmlNode data)
        {
            try
            {
                if (date.Attributes["text"] != null)
                {
                    var texts = GetValues("text", date, data);
                    foreach (string text in texts)
                    {
                        _output.AddPredicateAndLiteral(resourceNode.xmlResouceNode, "rdfs", "text", SanitiseString(text));
                    }
                }
                if (date.Attributes["comment"] != null)
                {
                    var comments = GetValues("comment", date, data);
                    foreach (string comment in comments)
                    {
                        _output.AddPredicateAndLiteral(resourceNode.xmlResouceNode, "rdfs", "comment", SanitiseString(comment));
                    }
                }
                if (date.Attributes["earliest"] != null)
                {
                    var earliests = GetValues("earliest", date, data);
                    foreach (string earliest in earliests)
                    {
                        _output.AddPredicateAndLiteral(resourceNode.xmlResouceNode, "bmx", "PX.time-span_earliest",
                                                       SanitiseString(earliest));
                        _output.AddPredicateAndLiteralWithType(resourceNode.xmlResouceNode, "bmx", "time-span_earliest_int",
                                                               SanitiseString(DateToInt(earliest)),
                                                               ELiteralType.XsdInteger);
                    }
                }
                if (date.Attributes["latest"] != null)
                {
                    var latests = GetValues("latest", date, data);
                    foreach (string latest in latests)
                    {
                        _output.AddPredicateAndLiteral(resourceNode.xmlResouceNode, "bmx", "PX.time-span_latest",
                                                       SanitiseString(latest));
                        _output.AddPredicateAndLiteralWithType(resourceNode.xmlResouceNode, "bmx", "PX.time-span_latest_int",
                                                               SanitiseString(DateToInt(latest)),
                                                               ELiteralType.XsdInteger);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in Date tag: " + date.OuterXml + " " + ex.Message + (_includeStackTraceInOutput ? ex.StackTrace : ""));
            }
        }*/
        /// <summary>
        //  this function takes a date and returns an integer in the form [-]YYYYMMDD
        //  input currently handled:
        //
        //    DD MMM YYYY
        //    Y*
        //    Y* BC
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private string DateToInt(string date)
        {
            var dateCpy = date.Trim();

            //DD MMM YYYY
            if(dateCpy.Length == 11 && dateCpy[2] == ' ' && dateCpy[6] == ' ')
            {
                return dateCpy.Substring(dateCpy.Length - 4) + _months[dateCpy.Substring(3, 3)] + dateCpy.Substring(0, 2);
            }
            
            //  Y* BC
            if(dateCpy.Substring(dateCpy.Length-3) == " BC")
            {
                return "-" + dateCpy.Substring(0, dateCpy.Length - 3);
            }

            //  Y*
            return dateCpy + "0000";
        }

        /// <summary>
        /// sanitise a string -- remove wacky entities and characters
        /// </summary>
        /// <param name="value"></param>
        private string SanitiseString(string value) 
        {            
            var newValue = value;
            var delete = new char[4] { (char)11, (char)14, (char)19, (char)29 };
            foreach(char c in delete)
            {
                newValue = newValue.Replace(c.ToString(), "");
            }

            var search = new string[3] { "'", "<", ">" };
            var replace = new string[3] { "’", "&lt", "&gt" };
            for (int i=0; i<search.Length; i++) 
            {
                newValue.Replace(search[i], replace[i]);
            }
            //if (newValue.Contains("&")) 
            //{
                  //TODO:entities2Accents
            //}
            return newValue;
        }



        private void ShortUri(string value, out string prefix, out string shortUri) 
        {
            prefix = string.Empty;

            //set the shortUri to default to value
            shortUri = value;

            foreach (string pf in _namespaces.Keys) 
            {
                var uri = _namespaces[pf];
                if (value.StartsWith(uri))
                {
                    prefix = pf;
                    shortUri = value.Substring(uri.Length);
                    break;
                }
            }
        }
        private string SanitiseUri(string value) 
        {
            //  delete these characters
            string newValue = value.Replace("<", "").Replace(">","");

            //  replace anything else wacky with a -
            newValue = Regex.Replace(newValue,"[^a-zA-Z0-9_#\\-/:\\.]","-");

            //  remove leading, ending or double hyphens
            while (newValue.Contains("--"))
            {
                newValue = newValue.Replace("--", "-");
            }

            if(newValue.StartsWith("-"))
                newValue = newValue.Substring(1);
            if(newValue.EndsWith("-"))
                newValue = newValue.Substring(0,newValue.Length -1);

            //  remove double slashes, presuming not preceeded by http:
            newValue = Regex.Replace(newValue,"([^:])//","\\1/");

            //  if there is a trailing slash remove it, unless just http://domain.com/
            //  ie greater than three /

            if (newValue.EndsWith("/") && CountStringOccurrences(newValue, "/") > 3)
                newValue = newValue.Substring(0, newValue.Length - 1);

            return newValue;

        }

       private string[] GetValues(string attr, XmlNode idNode, XmlNode data, ModifierDelegate mod_func = null, int min=0, int max=0)
        {
            //GetValues2(attr, idNode, data, mod_func, min, max);

            List<string> toReturn = new List<string>();

            var parent = idNode.ParentNode;
            if(min > 0 && idNode.Attributes[attr] == null)
                throw new Exception("Missing attribute " + attr + "=\"...\" in: " + idNode.OuterXml);

            var value = idNode.Attributes[attr].Value;
            var prefix = idNode.Attributes["prefix"] != null ?  idNode.Attributes["prefix"].Value : "";

            //is there an xpath expression?
            var xpaths1 = Regex.Matches(value, "{(.*?)}");

            var internalFuncCalls = Regex.Matches(value, "\\^(.*?)~");
            List<Match> xpaths = new List<Match>();

            //We'll add the internalFuncCalls first so their are computed first (really we want to create a parse tree but will mean a v2)
            xpaths.AddRange(internalFuncCalls.Cast<Match>());
            xpaths.AddRange(xpaths1.Cast<Match>());
            

            if (/*value.Contains("{") &&*/ xpaths.Count > 0) 
            {
                //  if there is more than one xpath to look up in this value,
                //  object-{obj_id}-{part_id} then we want only one match for
                //  each xpath term
                if (xpaths.Count > 1 || value.Substring(0, 1) != "{" || value.Substring(value.Length - 1, 1) != "}")
                {
                    bool matchedSomething = false;
                    foreach (Match xPath in xpaths)
                    {
                        /*If we are here then we have the situation of multiple xpath queriers: xxx-{xpath1}xxxx{xpath2}
                          If so, before we continue, we need to rule out any sub calls to internal functions such as:
                        {/xpath[{counter_CounterVariable}]}
                     */
                        string xPathValue = xPath.Value;
                        //MatchCollection xpaths2 = Regex.Matches(RemoveClosingBrackets(xPathValue), "\\^(.*?)\\~");
                        //foreach (Match m in xpaths2)
                        //{
                        //    string result2;
                        //    if (TryInternalFunction(RemoveClosingBrackets(m.Value), out result2))
                        //    {
                        //        xPathValue = xPathValue.Replace(m.Value, result2);
                        //    }
                        //}
                        xPathValue = MatchNestedInternalFunctionCalls(xPathValue);

                        string result;
                        if (TryInternalFunction(RemoveClosingBrackets(xPathValue), out result))
                        {
                            value = value.Replace(xPathValue, result);
                        }
                        else
                        {
                            XPathNavigator nav = data.CreateNavigator();
                            XPathExpression expr = nav.Compile(RemoveClosingBrackets(xPathValue));
                            expr.SetContext(_namespaceManager);

                            //XPathNodeIterator iterator = nav.Select(expr);
                            //var iterator2 = nav.Evaluate(expr);
                            switch (expr.ReturnType)
                            {
                                case XPathResultType.NodeSet:
                                    XPathNodeIterator iterator = (XPathNodeIterator) nav.Select(expr);
                                    if (iterator.Count == 1 || (xpaths.Count == 1 && iterator.Count > 0))
                                    {
                                        iterator.MoveNext();

                                        if (iterator.Current.Value.Trim().Length > 0)
                                            matchedSomething = true;
                                        value = value.Replace(xPathValue, iterator.Current.Value);
                                    }
                                    else if (xpaths.Count != 1 && iterator.Count == 0)
                                    {
                                        throw new Exception("No match for " + xPathValue + " in " + attr + "=\"" +
                                                            value + "\"");
                                    }
                                    else if (xpaths.Count > 1)
                                    {
                                        throw new Exception("Multiple matches for " + xPathValue + " in " + attr +
                                                            "=\"" + value + "\"");
                                    }
                                    break;
                                case XPathResultType.String:
                                    string st = (string) nav.Evaluate(expr);
                                    if (!string.IsNullOrEmpty(st) && st.Trim().Length > 0)
                                    {
                                        matchedSomething = true;
                                        value = value.Replace(xPathValue, st);
                                    }
                                    break;
                                case XPathResultType.Number:
                                    //Assume it's a double
                                    double dNumber = (double) nav.Evaluate(expr);
                                    int iNumber;
                                    matchedSomething = true;
                                    //if it can be an int, then make it so...
                                    if (int.TryParse(dNumber.ToString(), out iNumber))
                                    {
                                        value = value.Replace(xPathValue, iNumber.ToString());
                                    }
                                    else
                                    {
                                        value = value.Replace(xPathValue, dNumber.ToString());
                                    }
                                    break;
                                default:
                                    throw new Exception("No match for " + xPathValue + " in " + attr + "=\"" + value +
                                                        "\"");
                            }
                        }

                    }
                    if (mod_func != null)
                        value = mod_func(value);

                    if (matchedSomething && !string.IsNullOrEmpty(value.Trim()))
                        toReturn.Add(prefix + value);

                }
                else {

                    /*If we are here then we don't have the situation of multiple xpath queriers: xxx-{xpath1}xxxx{xpath2}
                    Therefore we have just a normal bracketed xpath.  If so, before we continue, we need to rule out any sub calls to internal functions such as:
                        {/xpath[{counter_CounterVariable}]}
                     */
                    //MatchCollection xpaths2 = Regex.Matches(value, "\\^(.*?)~");
                    //foreach (Match m in xpaths2)
                    //{
                    //    string result2;
                    //    if (TryInternalFunction(RemoveClosingBrackets(m.Value), out result2))
                    //    {
                    //        value = value.Replace(m.Value, result2);
                    //    }
                    //}
                    value = MatchNestedInternalFunctionCalls(value);


                    //  quite happy to enumerate for all values which match the xpath
                    var xpath = RemoveClosingBrackets(value);
                    string result;
                    if (TryInternalFunction(xpath, out result))
                    {
                        value = value.Replace(value, result);
                        if (mod_func != null)
                            value = mod_func(value);

                        if (!string.IsNullOrEmpty(value.Trim()))
                            toReturn.Add(prefix + value);
                    }
                    else
                    {
                        var matches = data.SelectNodes(xpath, _namespaceManager);
                        foreach (XmlNode node in matches)
                        {
                            //  ignore empty strings
                            if (!string.IsNullOrEmpty(node.InnerText))
                            {
                                //  apply modification function if required
                                var v = value.Replace("{" + xpath + "}", node.InnerText);
                                if (mod_func != null)
                                    v = mod_func(v);
                                if (!string.IsNullOrEmpty(v.Trim()))
                                    toReturn.Add(prefix + v);
                            }
                        }
                    }
                }
            }
            else{
                //  no xpath shenanigans, just return the content of the attribute
                if (mod_func != null)
                    value = mod_func(value);
                if (!string.IsNullOrEmpty(value.Trim()))
                    toReturn.Add(prefix + value);
            }

            //  check results between $min and $max
            if (toReturn.Count < min)
                throw new Exception("Unable to match " + attr + "=\"" + value + "\" \n\n   Caused by config node ");
            if (toReturn.Count > max && max > 0)
                throw new Exception("Too many attributes " + attr + "=\"\"");

            return toReturn.ToArray();
        }

        private string MatchNestedInternalFunctionCalls(string xPathValue)
        {
            MatchCollection xpaths2 = Regex.Matches(RemoveClosingBrackets(xPathValue), "\\^(.*?)\\~");
            foreach (Match m in xpaths2)
            {
                string result2;
                if (TryInternalFunction(RemoveClosingBrackets(m.Value), out result2))
                {
                    xPathValue = xPathValue.Replace(m.Value, result2);
                }
            }
            return xPathValue;
        }

        /// <summary>
        /// Get an array of child nodes with the given name
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private XmlNode[] GetChildren(XmlNode node, string name) 
        {
            List<XmlNode> nodes = new List<XmlNode>();
            foreach (XmlNode childNode in node.ChildNodes) 
            {
                if (childNode.Name.ToLower() == name.ToLower())
                    nodes.Add(childNode);
            }
            return nodes.ToArray();
        }
        private string RemoveClosingBrackets(string match) 
        {
            var matchNew = match;
            if (matchNew.StartsWith("{") || matchNew.StartsWith("^"))
                matchNew = matchNew.Remove(0, 1);
            if (matchNew.EndsWith("}") || matchNew.EndsWith("~"))
                matchNew = matchNew.Remove(matchNew.Length - 1, 1);
            return matchNew;
        }

        private Dictionary<string,string> GetMonths()
        {
            var months = new Dictionary<string, string>();
            months.Add("Jan", "01");
            months.Add("Feb", "02");
            months.Add("Mar", "03");
            months.Add("Apr", "04");
            months.Add("May", "05");
            months.Add("Jun", "06");
            months.Add("Jul", "07");
            months.Add("Aug", "08");
            months.Add("Sep", "09");
            months.Add("Oct", "10");
            months.Add("Nov", "11");
            months.Add("Dec", "12");
            return months;
        }

        public static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
        }

        private XmlNamespaceManager GetNamespacesFromData(XmlDocument data, string defaultNamespacePrefix)
        {
            var namespaceManager = new XmlNamespaceManager(data.NameTable);
            if (data.DocumentElement != null)
            {
                namespaceManager.AddNamespace(defaultNamespacePrefix, data.DocumentElement.NamespaceURI);
                AddNamespaceFromNode(data.ChildNodes, namespaceManager);
            }
            return namespaceManager;
        }
        private void AddNamespaceFromNode(XmlNodeList nodes, XmlNamespaceManager namespaceManager)
        {
            foreach (XmlNode node in nodes)
            {
                if (!string.IsNullOrEmpty(node.Prefix))
                {
                    if (!namespaceManager.HasNamespace(node.Prefix))
                        namespaceManager.AddNamespace(node.Prefix, node.GetNamespaceOfPrefix(node.Prefix));
                }
                else if (node.Attributes != null)
                {
                    if (node.Attributes.Count > 0)
                    {
                        foreach (XmlAttribute attribute in node.Attributes)
                        {
                            //if it is a namespace attribute, then we'll add it
                            if (attribute.Name.ToLower().StartsWith("xmlns:"))
                            {
                                if (!namespaceManager.HasNamespace(attribute.LocalName))
                                    namespaceManager.AddNamespace(attribute.LocalName, attribute.Value);
                            }
                        }
                    }
                }
                if (node.HasChildNodes)
                    AddNamespaceFromNode(node.ChildNodes, namespaceManager);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            _output.Dispose();

            _data = null;
            _config = null;
            _output = null;

            _namespaces.Clear();
            _months.Clear();
            _namespaceManager = null;

            _counters.Clear();
            _uniqueIdentifiers.Clear();

            _namespaces = null;
            _months = null;
            _counters = null;
            _uniqueIdentifiers = null;
        }

        #endregion
    }
}
