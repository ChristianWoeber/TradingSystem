using System;
using HelperLibrary.Calculations;
using HelperLibrary.Collections;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Interfaces
{
    /// <summary>
    /// PriceHistroyCollection Interface
    /// </summary>
    public interface IPriceHistoryCollection
    {
        /// <summary>
        /// the Calculation Context, which holds all calculation logic
        /// </summary>
        CalculationContext Calc { get; }


        /// <summary>
        /// The method that Gets a specific Record
        /// </summary>
        /// <param name="asof">the Datetime</param>
        /// <param name="option">the option</param>
        /// <param name="count">the recusive count  </param>
        /// <returns></returns>
        ITradingRecord Get(DateTime asof, PriceHistoryOption option = PriceHistoryOption.PreviousItem, int count = 0);


        /// <summary>
        /// The Count of the Items
        /// </summary>
        int Count { get; }

        /// <summary>
        /// The Unterlying Security Id
        /// </summary>
        int SecurityId { get; }
    }
}