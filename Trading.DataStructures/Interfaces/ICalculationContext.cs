using System;
using System.Collections.Generic;
using Trading.DataStructures.Enums;

namespace Trading.DataStructures.Interfaces
{
    public interface ICalculationContext
    {
        /// <summary>
        /// Berechnet den absoluten Return einer Periode
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="option"></param>
        /// <param name="priceHistoryOption"></param>
        /// <returns></returns>
        decimal GetAbsoluteReturn(DateTime from, DateTime? to = null, CaclulationOption? option = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem);

        /// <summary>
        /// Versucht den daily Return zu dem Zeitpunkt zurückzugeben
        /// </summary>
        /// <param name="asof"></param>
        /// <param name="dailyReturn"></param>
        /// <returns></returns>
        bool TryGetDailyReturn(DateTime asof, out IDailyReturnMetaInfo dailyReturn);

        /// <summary>
        /// Berechnet den per annum Return
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        decimal GetAverageReturn(DateTime from, DateTime? to = null, CaclulationOption? option = null);

        decimal GetAverageReturnMonthly(DateTime from, DateTime? to = null, CaclulationOption? option = null);

        /// <summary>
        /// Berechnet den MaxDrawdown
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        decimal GetMaximumDrawdown(DateTime? from = null, DateTime? to = null, CaclulationOption? option = null);

        /// <summary>
        /// Berechnet die monatliche Volatitlität
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="opt"></param>
        /// <param name="priceHistoryOption"></param>
        /// <returns></returns>
        decimal GetVolatilityMonthly(DateTime? from, DateTime? to = null, CaclulationOption? opt = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem);

        /// <summary>
        /// Enumeriert die täglichen Returns, nur die Werte ohne Datum
        /// </summary>
        /// <returns></returns>
        IEnumerable<decimal> EnumDailyReturns();

        /// <summary>
        /// Enumeriert die täglichen Returns, inklusive MetaDaten
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDailyReturnMetaInfo> EnumDailyReturnMetaInfos();

        /// <summary>
        /// Enumeriert die monatlichen Returns, nur die Werte ohne Datum
        /// </summary>
        /// <returns></returns>
        IEnumerable<decimal> EnumMonthlyReturns();

        /// <summary>
        /// Enumeriert die täglichen Returns also DateTime,decimal Tuple
        /// </summary>
        /// <returns></returns>
        IEnumerable<Tuple<DateTime, decimal>> EnumDailyReturnsTuple();

        /// <summary>
        /// Gibt an ob es zu dem Zeitpunkt einErgbenis für die tägliche Volatilität gab
        /// </summary>
        /// <param name="asof">der Stichtag</param>
        /// <param name="volatility">das Ergbenis der Volatilitätsberechnung</param>
        /// <returns></returns>
        bool TryGetLastVolatility(DateTime asof, out decimal? volatility);

        /// <summary>
        /// Gibt an ob es zu dem Zeitpunkt ein neues Low gab
        /// </summary>
        /// <param name="currentDate">der Stichtag</param>
        /// <returns></returns>
        bool DateIsNewLow(DateTime currentDate);
        /// <summary>
        /// Gibt die LowMetaInfo zum Zeitpunkt zurück
        /// </summary>
        /// <param name="currentDate">der Stichtag</param>
        /// <param name="info">die LowMetaInfo</param>
        /// <returns></returns>
        bool TryGetLastLowInfo(DateTime currentDate, out ILowMetaInfo info);

        /// <summary>
        /// Gibt die Info über die Summe der nagativen und die Summe der postiven täglichen Returns zum Zeitpunkt zurück
        /// </summary>
        /// <param name="currentDate">der Stichtag</param>
        /// <param name="info">die AbsoluteLossesAndGainsMetaInfo</param>
        /// <returns></returns>
        bool TryGetLastAbsoluteLossAndGain(DateTime currentDate, out IAbsoluteLossesAndGainsMetaInfo info);

    }
}