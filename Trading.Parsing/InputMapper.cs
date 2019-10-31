using System;
using System.Reflection;
using Trading.Parsing.Extensions;

namespace Trading.Parsing
{
    public class InputMapper : Tuple<string, PropertyInfo>
    {
        public InputMapper(string keyWord, PropertyInfo propertyInfo, int sortIndex) : base(keyWord, propertyInfo)
        {
            SortIndex = sortIndex;
            SetterFunc = propertyInfo.CreateSetMethod();
            GetterFunc = propertyInfo.CreateGetMethod();
        }

        public Func<object, object> GetterFunc { get; }

        public Action<object, object> SetterFunc { get; }

        /// <summary>
        /// der Suchstring
        /// </summary>
        public string KeyWord => Item1;
        /// <summary>
        /// Die Propertyinfo
        /// </summary>
        public PropertyInfo PropertyInfo => Item2;

        /// <summary>
        /// der Index an dem das Feld gefunden wurde
        /// </summary>
        public int? MatchingIndex { get; set; }

        /// <summary>
        /// der Index an dem das Feld gefunden wurde
        /// </summary>
        public int SortIndex { get; }

        /// <summary>
        /// Gibt an ob ein mapping existiert
        /// </summary>
        public bool HasMapping => MatchingIndex != null;
    }
}