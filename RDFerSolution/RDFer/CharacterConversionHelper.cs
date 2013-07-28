using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    /// <summary>
    //  This file provides functions relating to the conversion	
    //  of extended character set to/from HTML entities
    //  eg
    //	entities2accents($string);
    //	accents2entities($string);
    //	writeUTF($hexCode);
    /// </summary>
    public class CharacterConversionHelper
    {
        private List<string> _htmlCruft = new List<string>();

        public CharacterConversionHelper() 
        {
            PopulateHtmlCruft();
        }

        /// <summary>
        //  additional wacky characters to be removed
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string EntitiesToAccents(string value)
        {
            string newValue = value;
            
            //  Firstly, remove erroneous html stuff
            newValue = ReplaceAll(_htmlCruft.ToArray(), "", newValue);

            //  If we have any kinda ampersand, then process
            if(newValue.Contains('&'))
            {
                //  reduce everything to single ampersands
                var ampersands = new string[10] {"&amp;amp;", "&#x0026;", "&amp;#38;", "&#38;", "&amp;;", "&amp;", "&Amp;", "&AMP;", "&mp;", "\\&"};
                newValue = ReplaceAll(ampersands,"&",newValue);
                newValue = newValue.Replace("&&","&");

                //  try to detect missing ampersands
                var ampersandMatches = Regex.Matches(newValue,"(.)([#]{0,1}[A-Za-z0-9]+);"); 
                //TODO: regex

                //  carefully inspect any remaining ampersands
                //var matches = Regex.Matches(newValue,"([^&#]*)&([#]{0,1}[^#<\"&;,\-'\(\) ]*)([;\"<]{0,1})");
                //foreach(Match match in matches)
                //{
                    
                //}

            }
            return newValue;
        }

        /// <summary>
        //  Also set up an array containing bits of
        //  HTML garbage that we want to remove
        /// </summary>
        private void PopulateHtmlCruft() 
        {
            _htmlCruft.Add("&lt;?Pub Fmt italic&gt;");
            _htmlCruft.Add("&lt;?Pub Fmt /italic&gt;");
            
            var elements =  new string[12] {"i", "b", "f", "tt", "sup", "supscrpt", "sub", "subscrpt", "inf", "italic", "bold", "inline-equation"};
            foreach (string e in elements) 
            {
                _htmlCruft.Add("<" + e + ">");
                _htmlCruft.Add("</" + e + ">");
                _htmlCruft.Add("&lt;" + e + "&gt;");
                _htmlCruft.Add("&lt;/" + e + "&gt;");
            }
        }

        private string ReplaceAll(string[] identifiedValues, string with, string inString) 
        {
            string newValue = string.Empty;

            foreach (string value in identifiedValues) 
            {
                newValue = inString.Replace(value, with);
            }
            return newValue;
        }
    }
}
