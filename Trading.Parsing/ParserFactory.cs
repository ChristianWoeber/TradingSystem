
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Xml.Serialization;
using Trading.DataStructures.Interfaces;
using Trading.Parsing.Attributes;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;

namespace Trading.Parsing
{
    public class SimpleTextParser
    {
        private static readonly Dictionary<Type, List<PropertyInfo>> _propertyInfoCache =
            new Dictionary<Type, List<PropertyInfo>>();

        private static readonly Dictionary<Type, InputMappingCollection> _inputMappingCache =
            new Dictionary<Type, InputMappingCollection>();

        static SimpleTextParser()
        {
            InitCaches();
        }

        private static void InitCaches()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && x.GetInterfaces().Any(t => t == typeof(IInputMappable))))
            {
                lock (_lockObj)
                {
                    if (!_propertyInfoCache.TryGetValue(type, out _))
                    {
                        _propertyInfoCache.Add(type, new List<PropertyInfo>());
                        _inputMappingCache.Add(type, new InputMappingCollection());
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
                        _inputMappingCache[type].Add(keyWord, new InputMapper(keyWord, propertyInfo, attr.SortIndex));
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

            //gibt die input-mappers zurück
            var inputMappingCollection = GetOrAddInputMappers<T>();

            if (inputMappingCollection == null)
                throw new ArgumentException();

            using (var rd = new StreamReader(path))
            {
                var rowIdx = -1;
                string line;

                while ((line = rd.ReadLine()) != null)
                {
                    rowIdx++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var fields = line.Split(';', '|')
                        .Select(x => x.Trim()).ToList();

                    //map header
                    if (!inputMappingCollection.HasMappings)
                    {
                        CreateMapping<T>(fields, inputMappingCollection);
                        continue;
                    }

                    //wenn die header string ungleich sind neu mappen
                    if (rowIdx == 0 && inputMappingCollection.RowHeaderFromMatchingIndex != line)
                    {
                        CreateMapping<T>(fields, inputMappingCollection);
                        continue;
                    }

                    //sonst wieder zu den values
                    if (rowIdx == 0)
                        continue;

                    //das object zurückgeben
                    yield return CreateAndPopulateValue<T>(inputMappingCollection, fields);
                }
            }
        }

        /// <summary>
        /// Gibt die Keywords auf Basis der Property Info und des InputMappings zurück und fügt sie hinzu falls noch nicht vorhanden
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static InputMappingCollection GetOrAddInputMappers<T>()
            where T : class
        {
            lock (_lockObj)
            {
                InputMappingCollection inputMappers = null;
                if (!_propertyInfoCache.TryGetValue(typeof(T), out var propertyInfos))
                {
                    propertyInfos = typeof(T).GetProperties().ToList();
                    _propertyInfoCache.Add(typeof(T), propertyInfos);
                }

                if (!_inputMappingCache.TryGetValue(typeof(T), out var output))
                {
                    inputMappers = new InputMappingCollection();
                    //Get Keywords vis Reflection for Mapping//
                    foreach (var item in propertyInfos)
                    {
                        var attr = item.GetCustomAttribute<InputMapping>();
                        if (attr == null)
                            continue;

                        foreach (var key in attr.KeyWords)
                            inputMappers.Add(key, new InputMapper(key, item, attr.SortIndex));
                    }

                    _inputMappingCache.Add(typeof(T), inputMappers);
                }
                //Nur wenn der output nicht null ist setzen, sonst habe ich den type dynamisch hinzugefügt
                if (output != null)
                    inputMappers = output;
                return inputMappers;
            }

        }

        public static List<T> GetListOfType<T>(string data) where T : class
        {
            var lsReturn = new List<T>();

            if (string.IsNullOrWhiteSpace(data))
                return new List<T>();

            var inputMappingCollection = GetOrAddInputMappers<T>();

            if (inputMappingCollection == null)
                throw new ArgumentException();

            using (var rd = new StringReader(data))
            {
                var rowIdx = -1;
                string line;
                while ((line = rd.ReadLine()) != null)
                {
                    rowIdx++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var fields = line.Split(';', '|')
                        .Select(x => x.Trim()).ToList();

                    //lock (_lockObj)
                    //{
                    //map header
                    if (!inputMappingCollection.HasMappings)
                    {
                        CreateMapping<T>(fields, inputMappingCollection);
                        continue;
                    }

                    //wenn die header string ungleich sind neu mappen
                    if (rowIdx == 0 && inputMappingCollection.RowHeaderFromMatchingIndex != line)
                    {
                        CreateMapping<T>(fields, inputMappingCollection);
                        continue;
                    }

                    //sonst wieder zu den values
                    if (rowIdx == 0)
                        continue;
                    //}

                    //erstellt das objekt und schreibt die Values hinein
                    var obj = CreateAndPopulateValue<T>(inputMappingCollection, fields);
                    lsReturn.Add(obj);
                }
            }

            return lsReturn;
        }

        private static void CreateMapping<T>(IList<string> fields, InputMappingCollection inputMappingCollection) where T : class
        {
            if (inputMappingCollection.HasMappings)
            {
                foreach (var value in inputMappingCollection.Values)
                    value.MatchingIndex = null;
            }

            //Wenn die fileds null sind hol ich sie mir aus dem PropertyInfo Chache
            foreach (var field in fields ?? (fields = _propertyInfoCache[typeof(T)].Select(x => x.Name).ToList()))
            {
                if (!inputMappingCollection.TryGetValue(field, out var mapper))
                    continue;
                mapper.MatchingIndex = fields.IndexOf(field);
            }
        }

        private static T CreateAndPopulateValue<T>(InputMappingCollection inputMappingCollection, IReadOnlyList<string> fields) where T : class
        {
            //lock (_lockObj)
            //{
            // Create Obj //
            var obj = Activator.CreateInstance<T>();

            // Set Value of Mapped Properties
            foreach (var mappedField in inputMappingCollection.EnumMatchedFields())
            {
                //den Value holden
                var value = fields[mappedField.MatchingIndex.Value];

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

            return obj;
            //}
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
                mappedField.SetterFunc(obj, Convert.ToDateTime(value, CultureInfo.CurrentCulture));
            }

            else if (propertyType == typeof(decimal))
            {
                mappedField.SetterFunc(obj, Convert.ToDecimal(value, CultureInfo.InvariantCulture));
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

        public static void AppendToFile<T>(T item, string path) where T : class
        {
            AppendToFile<T>(new List<T> { item }, path);
        }


        public static void AppendToFile<T>(IEnumerable<T> items, string path) where T : class
        {
            //Merke mir hier einmalig die PropertyInfos zu jedem Model
            if (!_propertyInfoCache.TryGetValue(typeof(T), out var properties))
            {
                properties = typeof(T).GetProperties().Where(x => x.GetCustomAttribute<InputMapping>() != null)
                   .OrderBy(x => x.GetCustomAttribute<InputMapping>().SortIndex)
                   .ToList();
                if (properties.Count == 0)
                    throw new ArgumentNullException(nameof(AppendToFile), $"Es konnte kein Mapping im Typen {typeof(T)} gefunden werden");
                _propertyInfoCache.Add(typeof(T), properties);
            }

            if (!_inputMappingCache.TryGetValue(typeof(T), out var mappings))
            {
                mappings = new InputMappingCollection();
                //Get Keywords vis Reflection for Mapping//
                foreach (var item in properties ?? throw new ArgumentException("Achtung properties dürfen nicht null sein"))
                {
                    var attr = item.GetCustomAttribute<InputMapping>();
                    if (attr == null)
                        continue;

                    foreach (var key in attr.KeyWords)
                        mappings.Add(key, new InputMapper(key, item, attr.SortIndex));
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

        private static void WriteLines<T>(IEnumerable<T> items, InputMappingCollection inputMappingCollection, StreamWriter writer)
        {
            foreach (var item in items)
            {
                var row = inputMappingCollection.CreateRowFromValue(item, ConvertValue);
                writer.WriteLine(row);
            }
        }

        private static void WriteToFile<T>(IEnumerable<T> items, string path, InputMappingCollection mappingsCollection) where T : class
        {
            if (!mappingsCollection.HasMappings)
            {
                CreateMapping<T>(null, mappingsCollection);
            }

            using (var writer = File.CreateText(path))
            {
                //ich hol mir die Header über die Linq aggregate Methode
                //vorher auf den Name ein select
                var header = mappingsCollection.RowHeader ?? CreateHeaderFromFirstItem(items.FirstOrDefault(), mappingsCollection);

                writer.WriteLine(header);

                if (items == null)
                    return;

                WriteLines(items, mappingsCollection, writer);
            }
        }

        private static string CreateHeaderFromFirstItem<T>(T item, InputMappingCollection mappingsCollection)
        {
            var prop = _propertyInfoCache[item.GetType()];

            for (var i = 0; i < prop.Count; i++)
            {
                var p = prop[i];
                if (p.GetValue(item) == null)
                    continue;
                if (!mappingsCollection.TryGetValue(p.Name, out var mapper))
                    throw new ArgumentOutOfRangeException($"wurde nicht gefunden {p}");
                mapper.MatchingIndex = i;
            }

            return mappingsCollection.RowHeader;
        }

        private static string ConvertValue(object value)
        {
            if (IsNumber(value))
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
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