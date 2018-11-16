using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HelperLibrary.Database.Models;
using HelperLibrary.Trading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Util.Converter
{
    public class TradingCandidateConverter : JsonConverter
    {
        public override bool CanWrite { get; } = false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            // Load JObject from stream
            var jsonObject = JObject.Load(reader);

            var candidate = new TradingCandidate();

            var candidateBaseJToken = jsonObject["_tradingCandidateBase"];

            var recordJToken = jsonObject["Record"];
            var scoreJToken = jsonObject["ScoringResult"];

            var tradingRecord = new TradingRecord();
            var scoringResult = new ConservativeScoringResult();

            serializer.Populate(recordJToken.CreateReader(), tradingRecord);
            serializer.Populate(scoreJToken.CreateReader(), scoringResult);

            return candidate;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPortfolioValuation);
        }
    }


    public class PortfolioValuationConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var o = new JObject { { "type", nameof(IPortfolioValuation) } };

            var interfacePropertyNames = typeof(IPortfolioValuation).GetProperties().ToList();

            foreach (var propertyInfo in value.GetType().GetProperties())
            {
                if (interfacePropertyNames.Any(p => p.Name == propertyInfo.Name))
                {
                    var propertyValue = propertyInfo.GetValue(value);
                    if (propertyValue == null)
                        continue;
                    o.Add(new JProperty(propertyInfo.Name, JToken.FromObject(propertyValue, serializer)));
                }
            }

            o.WriteTo(writer);

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            // Load JObject from stream

            var jsonObject = JObject.Load(reader);
            var valuation = new PortfolioValuation();
            serializer.Populate(jsonObject.CreateReader(), valuation);
            return valuation;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPortfolioValuation);
        }
    }


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
