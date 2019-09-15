using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Util.Converter
{
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

            //var jsonObject = JObject.Load(reader);
            //var valuation = new PortfolioValuation();
            //serializer.Populate(jsonObject.CreateReader(), valuation);
            //return valuation;
            //TODO:
            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPortfolioValuation);
        }
    }
}