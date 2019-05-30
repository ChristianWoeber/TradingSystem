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

        /// <summary>
        /// Methode um die Performance aller holdings mit zu tracen und auch in den Rebalacne score einfließen zu lassen
        /// </summary>
        /// <param name="currentPosition">die aktuelle Postion</param>
        /// <param name="price">der Preis</param>
        /// <param name="portfolioAsof">und das Proertfolio Datum</param>
        void UpdatePerformance(ITransaction currentPosition, decimal? price, DateTime portfolioAsof);

        /// <summary>
        /// gibt on ob die aktuelle Positon zu den Top 5 der aktuell performancestärksten Positionen zählt
        /// </summary>
        /// <param name="securityId"></param>
        /// <param name="count">die Anzahl 5 für die Top Five</param>
        /// <returns></returns>
        bool IsUnderTopPositions(int securityId,int count = 5);

        /// <summary>
        /// Sortiert das Performacne Dictionary
        /// </summary>
        void CreateSortedPerformanceDictionary();

        /// <summary>
        /// gibt die Performance des Underlyings zurück
        /// </summary>
        /// <param name="securityId">der Security id</param>
        /// <returns></returns>
        decimal? GetUnderlyingPerformance(int securityId);
    }
}