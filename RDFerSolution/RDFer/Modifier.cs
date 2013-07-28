using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using JoshanMahmud.SemanticWeb.ModifierLibrary;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    public class Modifier
    {
        #region IModifier Members

        public static ModifierDelegate GetModifierMethod(string methodName)
        {
            ModifierDelegate modifierDelegate = null;

            switch (methodName.Trim().ToLower())
            {
                case "md5":
                    modifierDelegate = new ModifierDelegate(Md5);
                    break;
                case "strtolower":
                    modifierDelegate = new ModifierDelegate(StrToLower);
                    break;
                case "extractcurrency":
                    modifierDelegate = new ModifierDelegate(ExtractCurrency);
                    break;
                case "extractdenomination":
                    modifierDelegate = new ModifierDelegate(ExtractDenomination);
                    break;
                default:
                    return GetCustomModifier(methodName);
            }
            return modifierDelegate;
        }

        #endregion

        #region Modifier Methods
        private static ModifierDelegate GetCustomModifier(string methodName)
        {
            var pathToModifiers = Properties.Settings.Default.modifiers;
            var customModifierDlls = Directory.GetFiles(pathToModifiers,"*.dll");

            foreach(string customModifierDll in customModifierDlls)
            {
                FileInfo fi = new FileInfo(customModifierDll);
                System.Reflection.Assembly myDllAssembly = System.Reflection.Assembly.LoadFile(fi.FullName);

                var instances = from t in myDllAssembly.GetTypes()
                                where t.GetInterfaces().Contains(typeof(IModifier))
                                         && t.GetConstructor(Type.EmptyTypes) != null
                                select Activator.CreateInstance(t) as IModifier;

                foreach (var instance in instances)
                {
                    var method = instance.GetCustomModifierMethod(methodName); // where Foo is a method of ISomething
                    if (method != null)
                        return method;
                }
            }
            //If we get here then the method was not found
            throw new Exception("Error in config: modifier function modifier=" + methodName + " does not exist.  If it is custom, please verify that you use the IModifier Interface from the ModifierLibrary.dll");
        }

        private static string Md5(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        private static string StrToLower(string input)
        {
            return input.ToLower();
        }
        /// <summary>
        /// extension for the BM
        /// extracts the first part of "currency,value"
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string ExtractCurrency(string input)
        {
            string value = input.Trim();

            if(string.IsNullOrEmpty(value))
            {
                return "";
            }
            int pos = value.IndexOf(',');
            if(pos > 0)
            {
                value = value.Substring(0,pos);
            }
            return value;
        }
        /// <summary>
        /// extension for the BM
        /// extracts the second part of "currency,value"
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string ExtractDenomination(string input)
        {
            string value = input.Trim();

            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            int pos = value.IndexOf(',');
            if (pos > 0)
            {
                value = value.Substring(pos+1);
                double num;
                if (!double.TryParse(value, out num))
                    value = "";
            }
            else
                value = "";

            return value;
        }

        #endregion
    }
}
