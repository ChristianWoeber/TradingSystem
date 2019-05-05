using System;

namespace Trading.DataStructures.Interfaces
{
    public interface IPositionWatchService
    {
        /// <summary>
        /// Methode für das Berechnen des LossLimits
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        bool HasStopLoss(ITradingCandidate candidate);

        /// <summary>
        /// Die Methode berechnet die Limits auf täglicher basis
        /// </summary>
        /// <param name="transactionItem"></param>
        /// <param name="price"></param>
        /// <param name="portfolioAsof"></param>
        void UpdateDailyLimits(ITransaction transactionItem, decimal? price, DateTime portfolioAsof);

        /// <summary>
        /// Die bekommt die transaktion und entscheidet auf Basis des Transaktionstypen ob sie hinzugefügt oder geadded wird
        /// </summary>
        /// <param name="transactionItem"></param>
        void AddOrRemoveDailyLimit(ITransaction transactionItem);

        /// <summary>
        /// Gibt zurück ab welchem Time-Lag in tagen die Positon wider ausgestoppt werden darf 
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        bool IsBelowMinimumStopHoldingPeriod(ITradingCandidate candidate);

        /// <summary>
        /// Gibt zum Stichtag die zugehörige StopLossMetaInfo zurück
        /// </summary>
        /// <param name="candidate">der TradingKandiate</param>
        /// <returns></returns>
        IStopLossMeta GetStopLossMeta(ITradingCandidate candidate);
    }
}