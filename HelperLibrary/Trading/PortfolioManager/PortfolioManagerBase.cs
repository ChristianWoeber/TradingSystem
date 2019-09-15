using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    /// <summary>
    /// Abstrakte BasisKlasse des PortfolioManagers die, die Kandidaten als auch die TransactionItems führt
    /// </summary>
    public abstract class PortfolioManagerBase : IPortfolioValuation
    {
        private DateTime _portfolioAsof;
        protected PortfolioManagerBase(IStopLossSettings stopLossSettings, IPortfolioSettings portfolioSettings, ITransactionsHandler handler)
        {
            StopLossSettings = stopLossSettings;
            PortfolioSettings = portfolioSettings;
            TransactionsHandler = handler;
        }


        public event EventHandler<DateTime> PortfolioAsofChangedEvent;

        /// <summary>
        /// die Einstellungen zu den StopLoss Limits
        /// </summary>
        public readonly IStopLossSettings StopLossSettings;

        /// <summary>
        /// die einstellungen zum Portfolio
        /// </summary>
        public readonly IPortfolioSettings PortfolioSettings;

        /// <summary>
        /// Der TransactionsHandler - Hilfsklasse zum querien und aufbereiten der Transaktionen
        /// </summary>
        public readonly ITransactionsHandler TransactionsHandler;

        /// <summary>
        /// Der Scorping Provider - gibt das Scoring zu jeder security zu einem bestimmten Zeitpunkt zurück
        /// </summary>
        public IScoringProvider ScoringProvider { get; set; }


        /// <summary>
        /// Das Stichtag der Betrachtung
        /// Wenn sich dieser ändert wird das event PortfolioAsofChanged gefeuert
        /// </summary>
        public DateTime PortfolioAsof
        {
            get => _portfolioAsof;
            set
            {
                _portfolioAsof = value;
                PortfolioAsofChangedEvent?.Invoke(this, PortfolioAsof);
            }
        }

        /// <summary>
        /// die Methode die alle Regeln anweden soll
        /// </summary>
        public abstract void ApplyPortfolioRules(List<ITradingCandidate> candidates);

        /// <summary>
        /// die Methode zum Evaluieren des gesamten Portfolios
        /// </summary>
        protected abstract IEnumerable<ITradingCandidateBase> RankCurrentPortfolio();

        /// <summary>
        /// To be implemented in the derived class for registering the scoring Provider
        /// </summary>
        /// <param name="scoringProvider">the scoring Provider</param>
        public abstract void RegisterScoringProvider(IScoringProvider scoringProvider);

        /// <summary>
        /// die Methode muss implementiert werden um den aktuellen Portfolio Wert zu berechnen
        /// </summary>
        /// <returns></returns>
        public abstract void CalculateCurrentPortfolioValue();

        /// <summary>
        /// Der Portfoliowert - wird initial mit 100 angemommen, sonfern kein Betrag gegeben ist
        /// </summary>
        public decimal PortfolioValue { get; set; }


        /// <summary>
        /// Die Aktienquote, bzw. die Allokation Wertpapiere in %
        /// </summary>
        public decimal AllocationToRisk { get; set; }

    }
}
