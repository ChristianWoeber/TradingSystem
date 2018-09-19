using System;
using HelperLibrary.Database.Models;
using HelperLibrary.Trading;

namespace HelperLibrary.Interfaces
{
    /// <summary>
    /// Interface Defining the StopLoss Settings
    /// </summary>
    public interface IStopLossSettings
    {
        /// <summary>
        /// der Wert um den die Position verringert wird
        /// </summary>
        double ReductionValue { get; }

        /// <summary>
        /// das StopLoss limit
        /// </summary>
        decimal LossLimit { get; set; }

        /// <summary>
        /// Methode für das Berechnen des LossLimits
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="averagePrice"></param>
        /// <returns></returns>
        bool HasStopLoss(TradingCandidate candidate, decimal? averagePrice);

        /// <summary>
        /// Die Methode berechnet die Limits auf täglicher basis
        /// </summary>
        /// <param name="transactionItem"></param>
        /// <param name="price"></param>
        /// <param name="portfolioAsof"></param>
        void UpdateDailyLimits(TransactionItem transactionItem, decimal? price, DateTime portfolioAsof);

        /// <summary>
        /// Die Methode berechnet die Limits auf täglicher basis
        /// </summary>
        /// <param name="transactionItem"></param>
        void AddOrRemoveDailyLimit(TransactionItem transactionItem);
    }
}