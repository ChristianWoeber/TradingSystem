using System;
using JetBrains.Annotations;
using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    public interface IAdjustmentProvider : IPortfolioValuation
    {
        /// <summary>
        /// Methode um anpassen des temporären portfolios
        /// </summary>
        /// <param name="candidate">der Trading candidate</param>   
        void AddToTemporaryPortfolio(ITradingCandidate candidate);

        /// <summary>
        /// der CashHandler
        /// </summary>
        ICashManager CashHandler { get; }

        /// <summary>
        /// Methode um anpassen des temporären portfolios
        /// Gibt true zurück wenn das Abschichten der einen Position genug cash produziert
        /// </summary>
        /// <param name="missingCash"></param>
        /// <param name="candidate">der Trading candidate</param>
        /// <param name="adjustTargetWeightOnly"></param>
        bool AdjustTemporaryPortfolioToCashPuffer(decimal missingCash, ITradingCandidate candidate, bool adjustTargetWeightOnly = false);

        /// <summary>
        /// gibt zurück ob der Candidate unter dem Minum der Holding Periode ist
        /// </summary>
        /// <param name="currentWorstInvestedCandidate"></param>
        /// <returns></returns>
        bool IsBelowMinimumHoldingPeriode(ITradingCandidate currentWorstInvestedCandidate);

        /// <summary>
        /// adjusted den Trading Candidate für den Verkauf
        /// </summary>
        /// <param name="currentWeight">das aktuelle gewicht auf bais dessen die höhe des Verkaufs ermittelt wird</param>
        /// <param name="currentWorstInvestedCandidate"></param>
        void AdjustTradingCandidateSell(decimal currentWeight, ITradingCandidate currentWorstInvestedCandidate);


        /// <summary>
        /// die Aktuelle Auslastung
        /// </summary>
        decimal CurrentSumInvestedEffectiveWeight { get; }


        /// <summary>
        /// Die Temporären Kandiaten
        /// </summary>
        /// <returns></returns>
        Dictionary<int, ITradingCandidate> TemporaryCandidates { get; }

        /// <summary>
        /// Das Temporäre Portfolio
        /// </summary>
        ITemporaryPortfolio TemporaryPortfolio { get; }

        /// <summary>
        /// die MindestRiskogrenze inklusive Puffer
        /// </summary>
        decimal MinimumBoundary { get; }

        /// <summary>
        /// die maximale Riskogrenze inklusive Puffer
        /// </summary>
        decimal MaximumBoundary { get; }

        /// <summary>
        /// Der Scoring Provider
        /// </summary>
        IScoringProvider ScoringProvider { get; }

        /// <summary>
        /// Der Position Watcher
        /// </summary>
        IPositionWatchService PositionWatcher { get; }

    }
}