using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoshanMahmud.SemanticWeb.RdfConversion
{
    public class UniqueIdentifier
    {
        public string Name;

        private string _uniqueId;
        public string UniqueId
        {
            get { 
                if(string.IsNullOrEmpty(_uniqueId))
                    _uniqueId = GenerateUniqueId();
                
                return _uniqueId;
            }
        }

        public UniqueIdentifier(string name)
        {
            Name = name;
        }

        public void Generate()
        {
            _uniqueId = GenerateUniqueId();
        }

        private static string GenerateUniqueId()
        {
            long i = Guid.NewGuid().ToByteArray().Aggregate<byte, long>(1, (current, b) => current*((int) b + 1));
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }
    }
}
