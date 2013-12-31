using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    class Program
    {
        //params - required
        private static string _dataPath;
        private static string _configPath;
        private static string _outputPath;

        //params - optional (thus have default values)
        private static bool _split = false;
        private static string _inputChunkingFolder; //folder input for chunking
        private static string _outputChunkingFolder; //folder for output chunks
        private static ESplitType _splitType;
        private static int _splitSize;
        private static string _fileSuffixXpath = "";
        private static bool _pause = false; //this is to pause the application after it has finished
        private static bool _noisy = false; //this is to pause the application after it has finished
        private static string _format = "rdf";

        //This is for debug
        private static bool _includeStackTrackInOutput = false;

        //This is set after the _dataPath has been verified to be either a single file or directory of xml files
        private static bool _isSingleFile = false;
        private static bool _isDirectoryOfFiles = false;

        //constants - defaults
        private static string _inputDirectoryName = "inputchunks";
        private static string _outputDirectoryName = "outputchunks";

        static void Main(string[] args)
        {
            PrintTitle(); 
            GetAndCheckArgs(args);          

            //if we get here, all required params are met
            try
            {
                //no split
                if (!_split)
                {
                    Console.WriteLine("Starting RDFing...");

                    if (_isSingleFile)
                    {
                        var rdfER = new RDFer(_dataPath, _configPath, _outputPath, _includeStackTrackInOutput,_format);
                        rdfER.Start();
                        rdfER.Dispose();
                        rdfER = null; 
                    }
                    else if (_isDirectoryOfFiles) 
                    {
                        List<string> inputFiles = GetAllInputFiles(_dataPath, new List<string>());

                        foreach (string inputFile in inputFiles) 
                        {
                            Console.WriteLine("Processing File: " + inputFile);
                            var rdfER = new RDFer(inputFile, _configPath, _outputPath, _includeStackTrackInOutput, _format);
                            rdfER.Start();
                            rdfER.Dispose();
                            rdfER = null; 
                        }
                    }
                }
                else
                { 
                    //Split!!!!

                    //get the split details from config and setup
                    GetSplitConfig();

                    //Split the main data file into the number specified in the config
                    var splitter = new XmlSplitter(_dataPath, _inputChunkingFolder);

                    //do split
                    Console.WriteLine("Splitting original file...");
                    splitter.Split(_splitType, _splitSize,_fileSuffixXpath);
                    Console.Write(splitter.SplitTotal + " files.");
                    splitter = null;

                    //now that all of the files are split, lets iterate through each file and 
                    Console.WriteLine("Starting RDFing...");
                    //string[] splitFiles = ;
                    foreach (string split in Directory.GetFiles(_inputChunkingFolder)) 
                    {
                        if (_noisy)
                            Console.WriteLine("RDFing: " + split);

                        var rdfER = new RDFer(split, _configPath, _outputChunkingFolder, _includeStackTrackInOutput, _format);
                        rdfER.Start();
                        rdfER.Dispose();
                        rdfER = null;

                    } 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RDFing error:");
                Console.WriteLine(ex.Message + (_includeStackTrackInOutput ? ex.StackTrace : ""));
            }
            finally 
            {
                //if user requested application to pause then do so
                if (_pause)
                {
                    Console.WriteLine("RDFing finished.  Press return to end.");
                    Console.Read();
                }
                else 
                {
                    Console.WriteLine("RDFing finished");
                }
            }
            
        }
        private static List<string> GetAllInputFiles(string directory, List<string> files)
        {
            files.AddRange(Directory.GetFiles(directory));
            var subDirectories = Directory.GetDirectories(directory);
            foreach(var subDirectory in subDirectories)
            {
                GetAllInputFiles(subDirectory,files);
            }
            return files;
        }

        private static void GetAndCheckArgs(string[] args) 
        {
            Console.WriteLine("Checking arguments...");

            //Get args
            for (int i=0; i<args.Length; i++) 
            {
                switch (args[i]) 
                {
                    case "/c":
                    case "-c":
                    case "/config":
                    case "-config":
                        _configPath = args[i + 1];
                        i++;
                        break;
                    case "/i":
                    case "-i":
                    case "/input":
                    case "-input":
                        _dataPath = args[i + 1];
                        i++;
                        break;
                    case "/d":
                    case "-d":
                    case "/debug":
                    case "-debug":
                        _includeStackTrackInOutput = true;
                        break;
                    case "/o":
                    case "-o":
                    case "/output":
                    case "-output":
                        _outputPath = args[i + 1];
                        i++;
                        break;
                    case "/s":
                    case "-s":
                    case "/split":
                    case "-split":
                        _split = true;
                        break;
                    case "/p":
                    case "-p":
                    case "/pause":
                    case "-pause":
                        _pause = true;
                        break;
                    case "/n":
                    case "-n":
                    case "/noisy":
                    case "-noisy":
                        _noisy = true;
                        break;
                    case "/f":
                    case "-f":
                    case "/format":
                    case "-format":
                        _format = args[i + 1];
                        i++;
                        break;
                    default:
                        Usage();
                        break;
                }    
            }

            //Check args
            if (string.IsNullOrEmpty(_configPath) || string.IsNullOrEmpty(_dataPath) || string.IsNullOrEmpty(_outputPath))
                Usage();

            //Check files exist
            if(!File.Exists(_configPath))
                Abort("Config file (" + _configPath + ") specified does not exist");
            if (File.Exists(_dataPath) || Directory.Exists(_dataPath))
            {
                if (File.Exists(_dataPath))
                    _isSingleFile = true;

                if (Directory.Exists(_dataPath))
                    _isDirectoryOfFiles = true;
            }
            else
            {
                Abort("Input file/directory (" + _dataPath + ") specified does not exist");
            }
        }
        private static void PrintTitle() 
        {
            Console.WriteLine("********************************************************************");
            Console.WriteLine("*                RDFer - Converts XML to RDF                       *");
            Console.WriteLine("*                Author: Joshan Mahmud                             *");
            Console.WriteLine("********************************************************************");
            Console.WriteLine("\n\n");
        }
        private static void Usage() 
        {
            Console.Write("\n\n\t RDFer -c[/c /config] configfile.xml -i[/i /input] inputfile.xml> -o[/o /output]\nOPTIONAL\n\t-f[/f to specify format: rdf,ttl,n3,trig,nt,nq]\n\t-s[/s /split]\n\t-p[/p to pause]\n\t-n[/n output noise]\n\nYou must specify a config file as XML, an input file and a path to where to output the RDF files.\nUse the split flag to initiate the splitting of the XML file.");
            Environment.Exit(-1);
        }
        private static void Abort(string message) 
        {
            Console.WriteLine("\n\n ======== RDFer Error =======\n\n  " + message + "\n\n");
            Console.WriteLine(" =============================\n");
            Environment.Exit(-2);
        }
        private static void GetSplitConfig() 
        {
            var config = new System.Xml.XmlDocument();
            config.Load(_configPath);

            var split = config.SelectSingleNode("/config/split");
            
            if (split == null)
                throw new Exception("No <split> element defined in config.");

            if (split.Attributes["type"] == null || split.Attributes["size"] == null)
                throw new Exception("type, size, directoryname attributes were not defined for <split> element in config");
            
            //get config
            switch (split.Attributes["type"].Value.ToLower()) 
            {
                case "elementcount":
                    _splitType = ESplitType.ElementCount;
                    break;
                case "filesize":
                    _splitType = ESplitType.Filesize;
                    break;
                default:
                    throw new Exception("Unrecognized split type --> must be elementcount or filesize");
            }

            if (!int.TryParse(split.Attributes["size"].Value, out _splitSize))
                throw new Exception("size attribute not integer value");

            //now all config parameters obtained need to:
            //1) create a folder to place all of the chunks of origial data
            //2) find the folder for the output and name appropriately for each one

            if (split.Attributes["inputdirectoryname"] != null)
                _inputDirectoryName = split.Attributes["inputdirectoryname"].Value;
            if (split.Attributes["outputdirectoryname"] != null)
                _outputDirectoryName = split.Attributes["outputdirectoryname"].Value;
            if (split.Attributes["filesuffix"] != null)
                _fileSuffixXpath = split.Attributes["filesuffix"].Value;
            
            //input chunks folder - get the folder of the _dataPath
            FileInfo _dataFileInfo = new FileInfo(_dataPath);

            //_inputChunkingFolder =  _dataFileInfo.Directory.FullName + "\\" + _inputDirectoryName;
            _inputChunkingFolder = Path.Combine(_dataFileInfo.Directory.FullName, _inputDirectoryName);

            Directory.CreateDirectory(_inputChunkingFolder);

            //rdf output chunks folder - get the folder of the _outputPath
            _outputChunkingFolder = Path.Combine(_outputPath, _outputDirectoryName);
            Directory.CreateDirectory(_outputChunkingFolder);
        }
    }
}
