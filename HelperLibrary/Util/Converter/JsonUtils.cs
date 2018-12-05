using System.IO;
using Newtonsoft.Json;

namespace HelperLibrary.Util.Converter
{
    public static class JsonUtils
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
        };


        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented, _settings);
        }

        public static T Deserialize<T>(string serialized)
        {
            return (T)JsonConvert.DeserializeObject(serialized, typeof(T), _settings);
        }

        public static void SerializeToFile(object value, string filename = null, string intputPath = null)
        {
            string path;
            if (intputPath == null && filename == null)
                path = Path.GetTempFileName() + ".txt";
            else if (!string.IsNullOrWhiteSpace(filename))
            {
                path = Path.Combine(Path.GetTempPath(), filename);
            }
            else if (!string.IsNullOrWhiteSpace(intputPath))
            {
                path = Path.Combine(intputPath, Path.GetFileName(Path.GetTempFileName()));
            }
            else
            {
                path = Path.Combine(intputPath, filename);
            }

            File.WriteAllText(path, Serialize(value));
        }
    }
}