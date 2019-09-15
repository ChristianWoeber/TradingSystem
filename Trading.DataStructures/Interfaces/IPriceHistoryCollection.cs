using System;
using System.Collections.Generic;
using Trading.DataStructures.Enums;

namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// PriceHistroyCollection Interface
    /// </summary>
    public interface IPriceHistoryCollection : IEnumerable<ITradingRecord>
    {
        /// <summary>
        /// the Calculation Context, which holds all calculation logic
        /// </summary>
        ICalculationContext Calc { get; }


        /// <summary>
        /// The method that Gets a specific Record
        /// </summary>
        /// <param name="asof">the Datetime</param>
        /// <param name="option">the option</param>
        /// <param name="count">the recusive count  </param>
        /// <returns></returns>
        ITradingRecord Get(DateTime asof, PriceHistoryOption option = PriceHistoryOption.PreviousItem, int count = 0);


        /// <summary>
        /// The method that Gets a specific Record from its index
        /// </summary>
        /// <param name="index">the index</param>
        /// <param name="option">the option</param>
        /// <returns></returns>
        ITradingRecord Get(int index, PriceHistoryOption option = PriceHistoryOption.PreviousItem);


        /// <summary>
        /// The Count of the Items
        /// </summary>
        int Count { get; }

        /// <summary>
        /// The Unterlying Security Id
        /// </summary>
        int SecurityId { get; }

        /// <summary>
        /// die Settings zur PriceHistoryCollection falls vorhanden
        /// </summary>
        IPriceHistoryCollectionSettings Settings { get; }

        /// <summary>
        /// Der letzte Eintrag in der Collection
        /// </summary>
        ITradingRecord LastItem { get; }

        /// <summary>
        /// Der erste Eintrag in der Collection
        /// </summary>
        ITradingRecord FirstItem { get; }

        /// <summary>
        /// Gibt die Range des Angegebenen Zeitraums zurück
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        IEnumerable<ITradingRecord> Range(DateTime? from, DateTime? to, PriceHistoryOption option = PriceHistoryOption.PreviousItem);

        /// <summary>
        /// Gibt den täglich Return zum Datum des Records zurück
        /// </summary>
        /// <param name="indexLevel"></param>
        /// <returns></returns>
        decimal GetDailyReturn(ITradingRecord indexLevel);

        /// <summary>
        /// Enumeriert die Monats Ultimo Records
        /// </summary>
        /// <returns></returns>
        IEnumerable<ITradingRecord> EnumMonthlyUltimoItems();

        /// <summary>
        /// Gibt die LowMetaInfo zum Stichtag zurück
        /// </summary>
        /// <param name="asof"></param>
        /// <param name="lowMetaInfo"></param>
        /// <returns></returns>
        bool TryGetLowMetaInfo(DateTime asof, out ILowMetaInfo lowMetaInfo);

        /// <summary>
        /// Enumeriert die Lows
        /// </summary>
        /// <returns></returns>
        IEnumerable<(DateTime dateTime, ILowMetaInfo metaInfo)> EnumLows();
    }
}