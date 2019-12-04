using System;
using System.Collections.Generic;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf
{
    public class NewHighsCountScoringProvider : IScoringProvider
    {
        public NewHighsCountScoringProvider(Dictionary<int, IPriceHistoryCollection> createPriceHistoryFromSingleFiles)
        {
            PriceHistoryStorage = createPriceHistoryFromSingleFiles;
        }

        /// <summary>
        /// Storage of alle price histories, key => id, value the IPriceHistoryCollection
        /// </summary>
        public IDictionary<int, IPriceHistoryCollection> PriceHistoryStorage { get; }

        /// <summary>
        /// The Method that Returns the Scoring Result
        /// </summary>
        /// <param name="secId">the id of the security</param>
        /// <param name="date">the start date for the Calculations</param>
        public IScoringResult GetScore(int secId, DateTime date)
        {
            if (!PriceHistoryStorage.TryGetValue(secId, out var priceHistory))
                return new NewHighsCountScoringResult();

            //Das Datum des NAVs der in der PriceHistoryCollection gefunden wurde
            var priceHistoryRecordAsOf = priceHistory.Get(date)?.Asof;
            if (priceHistoryRecordAsOf == null)
                return new NewHighsCountScoringResult();

            //die 250 Tages Performance
            var performance250 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-250), date);

            //Wenn keine Berechungn der 250 Tages Performance möglich ist, returne ich false
            if (performance250 == -1)
                return new NewHighsCountScoringResult();

            //Alle Berechnungnen durchführen
            var performance10 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-10), date);
            var performance30 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-30), date);
            var performance90 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-90), date);
            if (!priceHistory.Calc.TryGetLastVolatilityInfo(date, out var volaInfo))
                return new NewHighsCountScoringResult();
            if (!priceHistory.TryGetLowMetaInfo(date, out var lowMetaInfo))
                return new NewHighsCountScoringResult();

            // Das Ergebnis returnen
            return new NewHighsCountScoringResult
            {
                Asof = priceHistoryRecordAsOf.Value,
                Performance10 = performance10,
                Performance30 = performance30,
                Performance90 = performance90,
                Performance250 = performance250,
                //MaxDrawdown = maxDrawDown,
                Volatility = volaInfo.DailyVolatility,
                LowMetaInfo = lowMetaInfo
            };
        }

        /// <summary>
        /// Returns then ITradingRecord at the given Date
        /// </summary>
        /// <param name="securityId">the security id</param>
        /// <param name="asof">The Datetime of the record</param>
        /// <returns></returns>
        public ITradingRecord GetTradingRecord(int securityId, DateTime asof)
        {
            return !PriceHistoryStorage.TryGetValue(securityId, out var priceHistoryCollection)
                ? null
                : priceHistoryCollection.Get(asof);
        }
    }
}