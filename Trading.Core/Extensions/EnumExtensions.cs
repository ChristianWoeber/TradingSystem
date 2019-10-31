using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Trading.Core.Extensions
{
    public static class EnumExtensions
    {
        private static readonly Dictionary<Type, List<(string EnumName, DescriptionAttribute Attribute)>> _descriptionCache = new Dictionary<Type, List<(string enumName, DescriptionAttribute description)>>();

        private static List<(string enumName, DescriptionAttribute description)> AddType(Enum source)
        {
            var ret = new List<(string enumName, DescriptionAttribute description)>();
            foreach (Enum enumValue in source.GetType().GetEnumValues())
            {
                var member = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
                ret.Add((enumValue.ToString(), member?.GetCustomAttribute<DescriptionAttribute>()));
            }
            _descriptionCache.Add(source.GetType(), ret);
            return ret;
        }

        public static string ToDescription(this Enum source)
        {
            if (!_descriptionCache.TryGetValue(source.GetType(), out var tupleAttributes))
                tupleAttributes = AddType(source);

            return tupleAttributes.FirstOrDefault(x => source.ToString() == x.EnumName).Attribute?.Description;
        }
    }
}