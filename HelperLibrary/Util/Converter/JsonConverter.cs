using System;
using System.Collections.Generic;
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
}
