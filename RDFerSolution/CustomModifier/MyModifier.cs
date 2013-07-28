using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JoshanMahmud.SemanticWeb.ModifierLibrary;

namespace CustomModifier
{
    public class MyModifier : IModifier
    {
        #region IModifier Members

        public ModifierDelegate GetCustomModifierMethod(string methodName)
        {
            ModifierDelegate md = null;
            switch (methodName)
            {
                case "encodevalue":
                    md = new ModifierDelegate(EncodeValue);
                    break;
                case "unitTomilliunit":
                    md= new ModifierDelegate(UnitToMilliUnit);
                    break;
                case "formatmerlinlatestdateasxsddate":
                    md = new ModifierDelegate(FormatMerlinLatestDateAsXsdDate);
                    break;
                case "formatmerlinearliestdateasxsddate":
                    md = new ModifierDelegate(FormatMerlinEarliestDateAsXsdDate);
                    break;
                case "settotitlecasingoflabel":
                    md = new ModifierDelegate(SetToTitleCasingOfLabel);
                    break;
            }
            return md;
        }
        public string EncodeValue(string input)
        {
            return input.Replace(" ", "_").Replace(".","_").Trim();
        }
        
        public string UnitToMilliUnit(string input)
        {
            double dt = Convert.ToDouble(input) / 1000;
            return dt.ToString();
        }

        public string FormatMerlinEarliestDateAsXsdDate(string input)
        {
            return FormatMerlinDateToXsdDateFormat(input, 1, 1);
        }
        public string FormatMerlinLatestDateAsXsdDate(string input)
        {
            return FormatMerlinDateToXsdDateFormat(input, 12, 31);
        }
        public string SetToTitleCasingOfLabel(string text)
        {
            return new CultureInfo("en-GB", false).TextInfo.ToTitleCase(text);
        }
        private string FormatMerlinDateToXsdDateFormat(string input, int month, int day)
        {
            /*
          * Acquisition date-ranges are rendered as D2 M3 Y4, e.g.  ‘23 May 2012’.
            Production date-ranges (where only information to the nearest year is deemed to be relevant) are rendered as Y4.  So the date above would be rendered as ‘2012’.

            Here are some examples:

            Acquisition Date:
            Text date = ‘1984’ , earliest date = ’01 Jan 2012’, latest date = ’31 Dec 2012’
            Text date = ‘1970-1980’, earliest date = ’01 Jan 1970’, latest date = ’31 Dec 1980’.
            Text date = ’23 Mar 2012 – 25 Mar 2012’, earliest date = ’23 Mar 2012’, latest date = ’25 Mar 2012’.

            Production date:

            Text date = ‘13thC’, earliest date = ‘1200’, latest date = ‘1299’
            Text date = ‘12thC BC’, earliest date =’1199 BC’, latest date = ‘1100 BC’
            Text date = ‘1100 BC – 100 AD’, earliest date = ‘1100 BC’, latest date = ‘0100’
            */


            if (string.IsNullOrEmpty(input))
                return input;

            //if contains BC or thC then it is a production date
            if (input.Contains("BC") || input.Contains("thC") || input.Length == 4)
            {
                return FormatProductionDateToXsdDateFormat(input, month, day);
            }
            else
            {
                return FormatAcquisitionDateToXsdDateFormat(input);
            }

        }

        private string FormatProductionDateToXsdDateFormat(string date, int month, int day)
        {
            int year = 0;
            string yearString = "";
            bool isBC = false;
            if (date.ToLower().Trim().Contains("bc"))
            {
                yearString = Regex.Match(date, @"\d+").Value;
                isBC = true;
            }
            else
                yearString = date.Trim();

            if (int.TryParse(yearString, out year))
            {
                //if the year is BC then append the minus to it
                string actualYear = isBC ? "-" + year.ToString("0000") : year.ToString("0000");
                return actualYear + "-" + month.ToString("00") + "-" + day.ToString("00");
            }
            return "";
        }

        private string FormatAcquisitionDateToXsdDateFormat(string date)
        {
            return DateTime.Parse(date).ToString("yyyy-MM-dd");
        }


        #endregion
    }
}
