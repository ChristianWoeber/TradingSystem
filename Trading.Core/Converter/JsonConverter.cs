using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Trading.Core.Candidates;
using Trading.Core.Models;
using Trading.Core.Scoring;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Converter
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

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IPortfolioValuation);
        }
    }
}
