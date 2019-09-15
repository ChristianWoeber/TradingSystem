using System;
using System.Linq;

namespace HelperLibrary.Yahoo
{
    public static class EnumExtensions
    {
        public static T GetAttribute<T>(this Enum val) where T : Attribute
        {
            var enumType = val.GetType();
            var name = Enum.GetName(enumType, val);
            return (T)enumType.
                GetField(name).
                GetCustomAttributes(typeof(T),false).              
                FirstOrDefault();
        }    
    }
}
