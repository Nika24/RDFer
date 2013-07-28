using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoshanMahmud.SemanticWeb.ModifierLibrary
{
    public delegate string ModifierDelegate(string value);

    public interface IModifier
    {
        ModifierDelegate GetCustomModifierMethod(string methodName);
    }
}
