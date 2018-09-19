using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;

namespace HelperLibrary.Trading.PortfolioManager
{

    public interface IPortfolioManager
    {
        /// <summary>
        /// Der Portfoliowert - wird initial mit 100 angemommen, sonfern kein Betrag gegeben ist
        /// </summary>
        decimal PortfolioValue { get; set; }


        /// <summary>
        /// Die Aktienquote, bzw. die Allokation der Wertpapiere in %
        /// </summary>
        decimal AllocationToRisk { get; set; }
    }

    /// <summary>
    /// Abstrakte BasisKlasse des PortfolioManagers die, die Kandidaten als auch die TransactionItems führt
    /// </summary>
    public abstract class PortfolioManagerBase : IPortfolioManager
    {
        protected PortfolioManagerBase(IStopLossSettings stopLossSettings, IPortfolioSettings portfolioSettings, ITransactionsHandler handler)
        {
            StopLossSettings = stopLossSettings;
            PortfolioSettings = portfolioSettings;
            TransactionsHandler = handler;
        }


        public event EventHandler<DateTime> PortfolioAsofChanged;

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
        protected IScoringProvider ScoringProvider;

        private DateTime _portfolioAsof;

        /// <summary>
        /// Das Stichtag der Betrachtung
        /// Wenn sich dieser ändert wird das event PortfolioAsofChanged gefeuert
        /// </summary>
        internal DateTime PortfolioAsof
        {
            get => _portfolioAsof;
            set
            {
                _portfolioAsof = value;
                PortfolioAsofChanged?.Invoke(this, PortfolioAsof);
            }
        }

        /// <summary>
        /// die Methode die alle Regeln anweden soll
        /// </summary>
        protected abstract void ApplyPortfolioRules(List<TradingCandidate> candidates);

        /// <summary>
        /// die Methode zum Evaluieren des gesamten Portfolios
        /// </summary>
        protected abstract IEnumerable<TradingCandidate> RankCurrentPortfolio();

        /// <summary>
        /// To be implemented in the derived class for registering the scoring Provider
        /// </summary>
        /// <param name="scoringProvider">the scoring Provider</param>
        public abstract void RegisterScoringProvider(IScoringProvider scoringProvider);

        /// <summary>
        /// die Methode muss implementiert werden um den aktuellen Portfolio Wert zu berechnen
        /// </summary>
        /// <returns></returns>
        protected abstract void CalculateCurrentPortfolioValue();

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
