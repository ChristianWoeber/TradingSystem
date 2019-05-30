
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using HelperLibrary.Database.Models;
using HelperLibrary.Util.Atrributes;
using HelperLibrary.Extensions;
using System;
using System.Linq;
using System.ServiceModel.Channels;
using System.Xml.Serialization;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Parsing
{
    public class SimpleTextParser
    {
        private static readonly Dictionary<object, List<PropertyInfo>> _propertyInfoCache =
            new Dictionary<object, List<PropertyInfo>>();

        private static readonly Dictionary<object, HashSet<InputMapper>> _inputMappingCache =
            new Dictionary<object, HashSet<InputMapper>>();

        static SimpleTextParser()
        {
            InitCaches();
        }

        private static void InitCaches()
        {
            foreach (var type in Assembly.GetAssembly(typeof(Transaction)).GetTypes().Where(x => x.IsClass && x.GetInterfaces().Any(t => t == typeof(IInputMappable))))
            {
                lock (_lockObj)
                {
                    if (!_propertyInfoCache.TryGetValue(type, out _))
                    {
                        _propertyInfoCache.Add(type, new List<PropertyInfo>());
                        _inputMappingCache.Add(type, new HashSet<InputMapper>());
                    }
                }
                foreach (var propertyInfo in type.GetProperties())
                {
                    //das attribute parsen
                    var attr = propertyInfo.GetCustomAttribute<InputMapping>();
                    if (attr == null)
                        continue;

                    //die propertyInfos einfügen
                    lock (_lockObj)
                    {
                        _propertyInfoCache[type].Add(propertyInfo);
                    }

                    //alle keywords einfügen
                    foreach (var keyWord in attr.KeyWords)
                        _inputMappingCache[type].Add(new InputMapper(keyWord, propertyInfo, attr.SortIndex));
                }
            }
        }

        public const string DELIMITER = ";";

        public static T GetSingleOfType<T>(string data)
        {
            foreach (var item in typeof(T).GetCustomAttributes(typeof(InputMapping), false))
            {

            }
            return default(T);
        }


        public static List<T> GetListOfType<T>(byte[] data, bool isZip = false) where T : class
        {
            using (var ms = new MemoryStream(data))
            {
                using (var zipArchiv = new ZipArchive(ms))
                {
                    foreach (var entry in zipArchiv.Entries)
                    {
                        if (Path.GetExtension(entry.FullName)?.Contains("csv") == true)
                        {
                            using (var rd = new StreamReader(entry.Open()))
                            {
                                return GetListOfType<T>(rd.ReadToEnd());
                            }
                        }
                    }
                }
            }
            return new List<T>();
        }

        public static List<T> GetListOfTypeFromFilePath<T>(string path) where T : class
        {
            return File.Exists(path)
                ? GetListOfType<T>(File.ReadAllText(path))
                : new List<T>();
        }

        /// <summary>
        /// Die IEnumerble Variante
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetItemsOfTypeFromFilePath<T>(string path) where T : class
        {
            return File.Exists(path)
                ? GetItemsOfType<T>(path)
                : throw new ArgumentException("!Der Pfad wurde nicht gefunden");
        }


        private static volatile object _lockObj = new object();


        private static IEnumerable<T> GetItemsOfType<T>(string path)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(path))
                yield break;

            //gibt die Keywords zurcük
            var keywords = GetOrAddKeywords<T>();

            if (keywords == null)
                throw new ArgumentException();

            //eine Liste mit den gefunden Mappings
            var foundKeys = new List<InputMapper>();

            using (var rd = File.OpenText(path))
            {
                string line;
                while ((line = rd.ReadLine()) != null)
                {
                    var fields = line.Split(';', '|')
                        .Select(x => x.Trim()).ToList();

                    //map header
                    if (foundKeys.Count == 0)
                    {
                        foreach (var k in keywords)
                        {
                            var match = fields.FirstOrDefault(x => x == k.KeyWord);
                            if (string.IsNullOrWhiteSpace(match))
                                continue;
                            k.ArrayIndex = fields.IndexOf(match);
                            foundKeys.Add(k);
                        }

                        continue;
                    }
                    //wenn kein Mapping fefunden wurde exception werfen
                    if (foundKeys.Count == 0)
                        throw new ArgumentException("Es konnte kein mapping hergestellt werden");

                    // Create Obj //
                    var obj = Activator.CreateInstance<T>();

                    // Set Value of Mapped Properties
                    foreach (var mappedField in foundKeys)
                    {
                        var value = fields[mappedField.ArrayIndex];

                        //Wenn er null oder empty ist den default wert ins model schreiben und weitergehen
                        if (value == "null" || string.IsNullOrEmpty(value))
                        {
                            mappedField.SetterFunc(obj, default(T));
                            continue;
                        }

                        //den Property Type holen & auch NUllables berücksichtigen
                        var propertyType = GetPropertyType(mappedField);
                        //und danach den Wert im Model setzen
                        SetPropertyValue(propertyType, mappedField, obj, value);
                        //das object zurückgeben
                        yield return obj;
                    }
                }
            }
        }

        /// <summary>
        /// Gibt die Keywords auf Basis der Property Info und des InputMappings zurück und fügt sie hinzu falls noch nicht vorhanden
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static HashSet<InputMapper> GetOrAddKeywords<T>()
            where T : class
        {
            HashSet<InputMapper> keywords;
            lock (_lockObj)
            {
                if (!_propertyInfoCache.TryGetValue(typeof(T), out var propertyInfos))
                {
                    propertyInfos = typeof(T).GetProperties().ToList();
                    _propertyInfoCache.Add(typeof(T), propertyInfos);
                }

                if (!_inputMappingCache.TryGetValue(typeof(T), out var output))
                {
                    keywords = new HashSet<InputMapper>();
                    //Get Keywords vis Reflection for Mapping//
                    foreach (var item in propertyInfos)
                    {
                        var attr = item.GetCustomAttribute<InputMapping>();
                        if (attr == null)
                            continue;

                        foreach (var key in attr.KeyWords)
                            keywords.Add(new InputMapper(key, item, attr.SortIndex));
                    }

                    _inputMappingCache.Add(typeof(T), keywords);
                }

                keywords = output;
            }

            return keywords;
        }

        public static List<T> GetListOfType<T>(string data) where T : class
        {
            //Action<T, object> setterFunc = null;
            var lsReturn = new List<T>();

            if (string.IsNullOrWhiteSpace(data))
                return new List<T>();

            var keywords = GetOrAddKeywords<T>();

            if (keywords == null)
                throw new ArgumentException();

            //eine Liste mit den gefunden Mappings
            var foundKeys = new List<InputMapper>();

            using (var rd = new StringReader(data))
            {
                string line;
                while ((line = rd.ReadLine()) != null)
                {
                    var fields = line.Split(';', '|')
                        .Select(x => x.Trim()).ToList();

                    //map header
                    if (foundKeys.Count == 0)
                    {
                        foreach (var k in keywords)
                        {
                            var match = fields.FirstOrDefault(x => x == k.KeyWord);
                            if (string.IsNullOrWhiteSpace(match))
                                continue;
                            k.ArrayIndex = fields.IndexOf(match);
                            foundKeys.Add(k);
                        }

                        continue;
                    }


                    //wenn kein Mapping fefunden wurde exception werfen
                    if (foundKeys.Count == 0)
                        throw new ArgumentException("Es konnte kein mapping hergestellt werden");

                    // Create Obj //
                    var obj = Activator.CreateInstance<T>();

                    // Set Value of Mapped Properties
                    foreach (var mappedField in foundKeys)
                    {
                        //den Value holden
                        var value = fields[mappedField.ArrayIndex];
                        //Wenn er null oder empty ist den default wert ins model schreiben und weitergehen
                        if (value == "null" || string.IsNullOrEmpty(value))
                        {
                            mappedField.SetterFunc(obj, default(T));
                            continue;
                        }

                        //den Property Type holen & auch NUllables berücksichtigen
                        var propertyType = GetPropertyType(mappedField);
                        //und danach den Wert im Model setzen
                        SetPropertyValue(propertyType, mappedField, obj, value);
                    }

                    lsReturn.Add(obj);
                }
            }

            return lsReturn;
        }

        /// <summary>
        /// Setzt den Property Wert im Model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyType"></param>
        /// <param name="mappedField"></param>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        private static void SetPropertyValue<T>(Type propertyType, InputMapper mappedField, T obj, string value) where T : class
        {
            if (propertyType == null)
                throw new ArgumentException(
                    $"Achtung es konnte kein Property Type extrahiert werden {mappedField.PropertyInfo.PropertyType}");

            if (propertyType == typeof(DateTime))
            {
                mappedField.SetterFunc(obj, Convert.ChangeType(value, propertyType, CultureInfo.CurrentCulture));
            }

            else if (propertyType == typeof(decimal))
            {
                mappedField.SetterFunc(obj, Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture));
            }
            else if (propertyType.BaseType == typeof(Enum))
            {
                if (int.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var intResult))
                    mappedField.SetterFunc(obj, (int)Enum.ToObject(propertyType, Convert.ToInt32(intResult)));
                else
                {
                    var parsedEnum = Enum.Parse(propertyType, value);
                    mappedField.SetterFunc(obj, parsedEnum);
                }
            }
            else
                mappedField.SetterFunc(obj, Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Gibt den Property Type zurück unter Berücksichtung von Nullable Values
        /// </summary>
        /// <param name="mappedField"></param>
        /// <returns></returns>
        private static Type GetPropertyType(InputMapper mappedField)
        {
            var nullableType = Nullable.GetUnderlyingType(mappedField.PropertyInfo.PropertyType);
            return nullableType != null ? nullableType : mappedField.PropertyInfo.PropertyType;
        }

        // n=name, o=open, p = previous close, s = symbol// 
        public static YahooDataRecordExtended GetSingleYahooLineHcMapping(string data)
        {
            var dataArray = data.Split(',', ';');

            if (dataArray[2].Contains("N/A") || dataArray[4].Contains("N/A"))
                return null;

            var name = Normalize(dataArray[0]);
            var close = ParseDecimal(dataArray[2]);
            var asof = ParseDateTime(dataArray[4]);

            return new YahooDataRecordExtended
            {
                Name = name,
                AdjustedPrice = close ?? Decimal.MinValue,
                Price = close ?? Decimal.MinValue,
                Asof = asof ?? DateTime.MinValue
            };
        }

        private static string Normalize(string input)
        {
            return input.Trim('\\', '"');
        }

        private static decimal? ParseDecimal(string input)
        {
            decimal d;
            if (decimal.TryParse(input, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                return d;

            return null;

        }

        private static DateTime? ParseDateTime(string input)
        {
            var regex = new Regex(@"\d\d?\/\d\d?\/\d\d\d\d");

            if (regex.IsMatch(input))
            {
                foreach (Match match in Regex.Matches(input, @"\d\d?\/\d\d?\/\d\d\d\d"))
                {
                    if (DateTime.TryParse(match.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date))
                    {
                        return date;
                    }

                    throw new ArgumentException("Achtung das DateTime konnte nicht geparsed werden!");

                }
            }

            return null;
        }

        public static string ReadFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Das File wurde nicht gefunden! Pfad:" + path);

            using (var reader = File.OpenText(path))
            {
                return reader.ReadToEnd();
            }

        }

        public static void AppendToFile<T>(T item, string path)
        {
            AppendToFile<T>(new List<T> { item }, path);
        }


        public static void AppendToFile<T>(IEnumerable<T> items, string path)
        {
            //Merke mir hier einmalig die PropertyInfos zu jedem Model

            if (!_propertyInfoCache.TryGetValue(typeof(T), out var properties))
            {
                properties = typeof(T).GetProperties().Where(x => x.GetCustomAttribute<InputMapping>() != null)
                   .OrderBy(x => x.GetCustomAttribute<InputMapping>().SortIndex)
                   .ToList();
                _propertyInfoCache.Add(typeof(T), properties);
            }

            if (!_inputMappingCache.TryGetValue(typeof(T), out var mappings))
            {
                mappings = new HashSet<InputMapper>();
                //Get Keywords vis Reflection for Mapping//
                foreach (var item in properties ?? throw new ArgumentException("Achtung properties dürfen nicht null sein"))
                {
                    var attr = item.GetCustomAttribute<InputMapping>();
                    if (attr == null)
                        continue;

                    foreach (var key in attr.KeyWords)
                        mappings.Add(new InputMapper(key, item, attr.SortIndex));
                }

                _inputMappingCache.Add(typeof(T), mappings);
            }

            if (mappings == null)
                throw new ArgumentException("Mappings dürfen nicht null sein!");

            if (!File.Exists(path))
            {
                WriteToFile(items, path, mappings);
            }
            else
            {
                using (var writer = File.AppendText(path))
                {
                    WriteLines(items, mappings, writer);
                }
            }
        }

        private static void WriteLines<T>(IEnumerable<T> items, HashSet<InputMapper> mappings, StreamWriter writer)
        {
            foreach (var item in items)
            {
                var row = mappings
                    .Select(i => i.GetterFunc(item))
                    .Select(ConvertValue)
                    .Aggregate((a, b) => a + DELIMITER + b);

                writer.WriteLine(row);
            }
        }

        private static void WriteToFile<T>(IEnumerable<T> items, string path, HashSet<InputMapper> mappings)
        {
            using (var writer = File.CreateText(path))
            {
                //ich hol mir die Header über die Linq aggregate Methode
                //vorher auf den Name ein select
                var header = mappings
                    .Select(x => x.PropertyInfo.Name)
                    .Aggregate((a, b) => a + DELIMITER + b);

                writer.WriteLine(header);

                if (items == null)
                    return;

                WriteLines(items, mappings, writer);
            }
        }

        private static string ConvertValue(object value)
        {
            if (IsNumber(value))
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (IsEnum(value))
                return Convert.ToString((int)Enum.ToObject(value.GetType(), value));
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? "null";
        }

        private static bool IsEnum(object value)
        {
            return value?.GetType().BaseType == typeof(Enum);
        }

        private static bool IsNumber(object value)
        {
            return value is sbyte
                   || value is byte
                   || value is short
                   || value is ushort
                   || value is int
                   || value is uint
                   || value is long
                   || value is ulong
                   || value is float
                   || value is double
                   || value is decimal;
        }

        public static T Deserialize<T>(string toDeserialize)
        {
            var xmlSerializer = new XmlSerializer(toDeserialize.GetType());
            using (var textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }

        public static string Serialize<T>(T toSerialize)
        {
            var xmlSerializer = new XmlSerializer(toSerialize.GetType());
            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

    }
}


//public class SetterFuncHelper<T>
//{
//    public SetterFuncHelper(InputMapper mappedField)
//    {
//        SetterFunc = mappedField.PropertyInfo.CreateSetter<T>();
//        MappedIndex = mappedField.ArrayIndex;
//    }

//    public Action<T, object> SetterFunc { get; }

//    public int MappedIndex { get; set; }
//}

public class InputMapper : Tuple<string, PropertyInfo>
{
    public InputMapper(string keyWord, PropertyInfo propertyInfo, int arrayIndex) : base(keyWord, propertyInfo)
    {
        ArrayIndex = arrayIndex;
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
    public int ArrayIndex { get; set; }

}




