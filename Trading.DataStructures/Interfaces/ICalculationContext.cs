using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        decimal GetAbsoluteReturn(DateTime from, DateTime? to = null, CalculationOption? option = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem);

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
        decimal GetAverageReturn(DateTime from, DateTime? to = null, CalculationOption? option = null);

        decimal GetAverageReturnMonthly(DateTime from, DateTime? to = null, CalculationOption? option = null);

        /// <summary>
        /// Berechnet den MaxDrawdown
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        decimal GetMaximumDrawdown(DateTime? from = null, DateTime? to = null, CalculationOption? option = null);

        /// <summary>
        /// Berechnet die monatliche Volatitlität
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="opt"></param>
        /// <param name="priceHistoryOption"></param>
        /// <returns></returns>
        decimal GetVolatilityMonthly(DateTime? from, DateTime? to = null, CalculationOption? opt = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem);

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

        /// <summary>
        /// Der Task berechnet die Rollierenden Ergebnisse auf Basis der PriceHistoryCollection
        /// </summary>
        /// <param name="periodesInYears">die perioden in Yahren die berechnet werden sollen</param>
        /// <returns></returns>
        Task CreateRollingPeriodeResultsTask(params int[] periodesInYears);

        /// <summary>
        /// Enumeriert alle verfügbaren ergebnisse zu den Rollierenden Perioden <see cref="CreateRollingPeriodeResultsTask"/>
        /// </summary>
        /// <returns></returns>
        IEnumerable<IEnumerable<IHistogrammCollection>> EnumHistogrammClasses();

        /// <summary>
        /// Gibt tur zurück wenn die LastVolatirly Info gefunden wurde und stellt die Metainfo dazu über das
        /// IMovingVolaMetaInfo Interface bereit
        /// </summary>
        /// <param name="currentDate"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        bool TryGetLastVolatilityInfo(DateTime currentDate, out IMovingVolaMetaInfo info);

        /// <summary>
        /// fügt den daily Return in die Collection hinzu
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        void AddDailyReturn(ITradingRecord from, ITradingRecord to);

        /// <summary>
        /// berechnet den laufenden arithmetischen Durchschnitt beim einfüllen der objekte in die collection
        /// </summary>
        /// <param name="record"></param>
        void CalcRunningArithmeticMean(ITradingRecord record);

        /// <summary>
        /// berechnet die laufenden lows auf basis der Einstellung in den Settings der PriceHistory Collection
        /// </summary>
        /// <param name="tradingRecord"></param>
        void CalcMovingLows(ITradingRecord tradingRecord);

        /// <summary>
        /// berechnet die Volatilität beim Einfügen der Records in die Collection
        /// </summary>
        /// <param name="tradingRecord"></param>
        void CalcMovingVola(ITradingRecord tradingRecord);

        /// <summary>
        /// berechnet die absoluten Verluste und Gewinne (netted quasi positive dailyReturns und negative)
        /// </summary>
        /// <param name="record"></param>
        void CalcAbsoluteLossesAndGains(ITradingRecord record);

        /// <summary>
        /// enumeriert die kvps der LowMetainfo Collecton als ValueTuples
        /// </summary>
        /// <returns></returns>
        IEnumerable<(DateTime dateTime, ILowMetaInfo metaInfo)> EnumLows();
    }
}