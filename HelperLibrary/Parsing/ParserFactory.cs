
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
using System.Windows.Forms.VisualStyles;
using System.Xml.Serialization;

namespace HelperLibrary.Parsing
{
    public class SimpleTextParser
    {

        public const string DELIMITER = ";";

        public static T GetSingleOfType<T>(string data)
        {
            foreach (var item in typeof(T).GetCustomAttributes(typeof(InputMapping), false))
            {

            }
            return default(T);
        }


        public static List<T> GetListOfType<T>(byte[] data, bool isZip = false)
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

        public static List<T> GetListOfTypeFromFilePath<T>(string path)
        {
            return File.Exists(path)
                ? GetListOfType<T>(File.ReadAllText(path))
                : new List<T>();
        }


        public static List<T> GetListOfType<T>(string data)
        {
            //Action<T, object> setterFunc = null;
            var lsReturn = new List<T>();

            if (string.IsNullOrWhiteSpace(data))
                return new List<T>();

            //initialize an empty HashSet with Key PropertyName and Value the input Mapping (the search keywords of the generic Type T
            var keywords = new HashSet<InputMapper<T>>();

            //initialize an empty Dictionary with Key PropertyName and Value Mapping
            var dicInputMapping = new Dictionary<string, TextReaderInputRecordMapping>(StringComparer.OrdinalIgnoreCase);

            //Get Keywords vis Reflection for Mapping//
            foreach (var item in typeof(T).GetProperties())
            {
                var attr = item.GetCustomAttributes(typeof(InputMapping), false);
                if (attr.Length <= 0)
                    continue;

                var mappingAttr = (InputMapping[])attr;
                foreach (var key in mappingAttr[0].KeyWords)
                {
                    keywords.Add(new InputMapper<T>(key, item));
                }
            }

            using (var rd = new StringReader(data))
            {
                var isFirst = true;
                string line;

                while ((line = rd.ReadLine()) != null)
                {
                    var fields = line.Split(';', '|');
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i].Trim();

                        if (string.IsNullOrWhiteSpace(field))
                            continue;

                        // map Header //
                        if (isFirst)
                        {
                            foreach (var keyword in keywords)
                            {
                                if (!keyword.PropertyName.ContainsIc(field))
                                    continue;
                                if (dicInputMapping.TryGetValue(keyword.PropertyName, out _))
                                    continue;
                                //Ich merke mir den konkreten propertyName für das Mapping
                                var mapping = new TextReaderInputRecordMapping(keyword.PropertyName, i);
                                dicInputMapping.Add(keyword.PropertyName, mapping);
                                break;
                            }
                        }
                        else
                        {
                            // Create Obj //
                            var obj = Activator.CreateInstance<T>();

                            // Set Value of Mapped Properties
                            //var insertToList = true;
                            foreach (var item in keywords)
                            {
                                if (!dicInputMapping.TryGetValue(item.PropertyName, out var mapping))
                                    continue;

                                var value = fields[mapping.ArrayIndex];

                                if (value == "null" || string.IsNullOrEmpty(value))
                                    item.SetterFunc(obj, default(T));

                                else if (item.PropertyInfo.PropertyType == typeof(DateTime))
                                {
                                    item.SetterFunc(obj, Convert.ChangeType(value, item.PropertyInfo.PropertyType,
                                        CultureInfo.CurrentCulture));
                                }
                                else if (item.PropertyInfo.PropertyType == typeof(decimal))
                                {
                                    item.SetterFunc(obj, Convert.ChangeType(value, item.PropertyInfo.PropertyType,
                                        CultureInfo.InvariantCulture));
                                }
                                else if (item.PropertyInfo.PropertyType.BaseType == typeof(Enum))
                                {
                                    if (int.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var intResult))
                                        item.SetterFunc(obj, (int)Enum.ToObject(item.PropertyInfo.PropertyType, Convert.ToInt32(intResult)));
                                    else
                                    {
                                        var parsedEnum = Enum.Parse(item.PropertyInfo.PropertyType, value);
                                        item.SetterFunc(obj, parsedEnum);
                                    }
                                }
                                else
                                    item.SetterFunc(obj, Convert.ChangeType(value, item.PropertyInfo.PropertyType,
                                        CultureInfo.InvariantCulture));
                            }

                            lsReturn.Add(obj);
                            break;
                        }
                    }

                    isFirst = false;

                }
                return lsReturn;
            }
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
            //TODO: Eventuell noch getter func statt der reflection
            var properties = typeof(T).GetProperties().Where(x => x.GetCustomAttribute<InputMapping>() != null).OrderBy(x => x.GetCustomAttribute<InputMapping>().SortIndex)
                ./*Select(x => new InputMapper<T>(x.Name, x)).*/ToList();

            var content = items.ToList();
            if (!File.Exists(path))
            {
                WriteToFile(content, path, properties);
            }
            else
            {
                using (var writer = File.AppendText(path))
                {
                    foreach (var item in content)
                    {
                        var row = properties
                            .Select(p => p.GetValue(item))
                            .Select(ConvertValue)
                            .Aggregate((a, b) => a + DELIMITER + b);

                        writer.WriteLine(row);
                    }
                }
            }
        }

        private static void WriteToFile<T>(IEnumerable<T> items, string path, List<PropertyInfo> properties)
        {
            using (var writer = File.CreateText(path))
            {
                //ich hol mir die Header über die Linq aggregate Methode
                //vorher auf den Name ein select
                var header = properties
                    .Select(x => x.Name)
                    .Aggregate((a, b) => a + DELIMITER + b);

                writer.WriteLine(header);

                if (items == null)
                    return;

                foreach (var item in items)
                {
                    var row = properties
                        .Select(p => p.GetValue(item))
                        .Select(ConvertValue)
                        .Aggregate((a, b) => a + DELIMITER + b);

                    writer.WriteLine(row);
                }
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
            return value.GetType().BaseType == typeof(Enum);
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

public class TextReaderInputRecordMapping : Tuple<string, int>
{
    public TextReaderInputRecordMapping(string propertyName, int arrayIndex) : base(propertyName, arrayIndex)
    {

    }
    public string PropertyName => Item1;
    public int ArrayIndex => Item2;


}

public class InputMapper<T> : Tuple<string, PropertyInfo>
{
    public InputMapper(string propertyName, PropertyInfo propertyInfo) : base(propertyName, propertyInfo)
    {
        SetterFunc = PropertyInfo.CreateSetter<T>();
    }
    public string PropertyName => Item1;
    public PropertyInfo PropertyInfo => Item2;
    public Action<T, object> SetterFunc { get; }
}

