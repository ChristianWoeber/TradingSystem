using System;
using System.Collections.Generic;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Scoring
{

    /// <summary>
    /// Der Default Scoring Provider - rechnet den Score und hat zugriff auf die Historien
    /// </summary>
    public class ScoringProvider : IScoringProvider
    {
        /// <summary>
        /// Das Dictionnary mit allen PriceHistoryCollections
        /// </summary>
        public IDictionary<int, IPriceHistoryCollection> PriceHistoryStorage { get; }


        public ScoringProvider(IDictionary<int, IPriceHistoryCollection> dictionary)
        {
            PriceHistoryStorage = dictionary;
        }


        public IScoringResult GetScore(int secId, DateTime date)
        {
            if (!PriceHistoryStorage.TryGetValue(secId, out var priceHistory))
                return new ConservativeScoringResult();

            //Das Datum des NAVs der in der PriceHistoryCollection gefunden wurde
            var priceHistoryRecordAsOf = priceHistory.Get(date)?.Asof;
            if (priceHistoryRecordAsOf == null)
                return new ConservativeScoringResult();

            //die 250 Tages Performance
            var performance250 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-250), date);

            //Wenn keine Berechungn der 250 Tages Performance möglich ist, returne ich false
            if (performance250 == -1)
                return new ConservativeScoringResult();

            ////Wenn es in den 250 Tagen ein Low gab, dass niederiger als die Vola ist, return ich ebenfalls false
            //if (priceHistory.Calc.ScanRangeNoLow(date.AddDays(-250), date))
            //    return new ScoringResult { IsValid = false };

            //Alle Berechnungnen durchführen
            var performance10 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-10), date);
            var performance30 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-30), date);
            var performance90 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-90), date);
            priceHistory.Calc.TryGetLastVolatilityInfo(date, out var volaInfo);
            priceHistory.Calc.TryGetLastAbsoluteLossAndGain(date, out var absoluteLossesAndGainsMetaInfo);

            //var maxDrawDown = priceHistory.Calc.GetMaximumDrawdown(date.AddDays(-250), date, CaclulationOption.Adjusted);

            // Das Ergebnis returnen
            var result = new ConservativeScoringResult
            {
                Asof = priceHistoryRecordAsOf.Value,
                Performance10 = performance10,
                Performance30 = performance30,
                Performance90 = performance90,
                Performance250 = performance250,
                //MaxDrawdown = maxDrawDown,
                Volatility = volaInfo.DailyVolatility,
                IsNewLow = priceHistory.Calc.DateIsNewLow(date),
                AbsoluteGainAndLossMetaInfo = absoluteLossesAndGainsMetaInfo

            };
            if (priceHistory.Calc.TryGetLastLowInfo(date, out var lowMetaInfo))
                result.LowMetaInfo = lowMetaInfo;

            return result;
        }

        public ITradingRecord GetTradingRecord(int securityId, DateTime asof)
        {
            return !PriceHistoryStorage.TryGetValue(securityId, out var priceHistoryCollection)
                ? null
                : priceHistoryCollection.Get(asof);
        }
    }
}
