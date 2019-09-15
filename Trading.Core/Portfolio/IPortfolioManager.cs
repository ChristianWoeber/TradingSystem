using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Trading.Core.Exposure;
using Trading.Core.Transactions;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Portfolio
{
    public interface IPortfolioManager : IAdjustmentProvider
    {
        /// <summary>
        /// der Risk Watcher - kümmert sich um die Berechnung der maximalen Aktienquote
        /// </summary>
        IExposureProvider AllocationToRiskWatcher { get; set; }

        /// <summary>
        /// der Rebalance Provider - kümmert sich um das Rebalanced des Portfolios
        /// </summary>
        IRebalanceProvider RebalanceProvider { get; set; }

        /// <summary>
        /// die Klasse die sich um die Berechnungen der Transaktion kümmert, Shares, Amount EUR etc..
        /// </summary>
        TransactionCalculationHandler TransactionCaclulationProvider { get; }

        /// <summary>
        /// Das aktuelle Portfolio (alle Transaktionen die nicht geschlossen sind)
        /// </summary>
        IEnumerable<ITransaction> CurrentPortfolio { get; }

        /// <summary>
        /// Flag das angibt ob es im temporären Portdolio zu Änderungen gekommen ist
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// das Event das aufgerufen wird, wenn eine position ausgestoppt wurde
        /// </summary>
        event EventHandler<PortfolioManagerEventArgs> StoppLossExecuted;

        /// <summary>
        /// das Event das aufgerufen wird, wenn sich die Positonen ändert
        /// </summary>
        event EventHandler<PortfolioManagerEventArgs> PositionChangedEvent;

        /// <summary>
        /// Event das aufgerufen wird wenn sich das Asof Datum des PMs ändert
        /// </summary>
        event EventHandler<DateTime> PortfolioAsofChangedEvent;

        /// <summary>
        /// Hier werden die Candidaten die zur Verfügung stehen injected
        /// </summary>
        /// <param name="candidatesBase"></param>
        /// <param name="asof">das Datum</param>
        void PassInCandidates([NotNull]List<ITradingCandidateBase> candidatesBase, DateTime asof);

        /// <summary>
        /// Die Haupt einstiegs Methode in den Portfolio Manager, wo alle Regeln angewandt werden
        /// </summary>
        /// <param name="candidates"></param>
        void ApplyPortfolioRules(List<ITradingCandidate> candidates);

        /// <summary>
        /// Anpassung des Kandidaten für das temporäre Portfolio und für die Transaktionserstellung
        /// </summary>
        /// <param name="currentWeight"></param>
        /// <param name="candidate"></param>
        void AdjustTradingCandidateBuy(decimal currentWeight, ITradingCandidate candidate);

        /// <summary>
        /// Registiert den Scoring Provider, der den Score berechnet
        /// </summary>
        /// <param name="scoringProvider"></param>
        void RegisterScoringProvider(IScoringProvider scoringProvider);

        /// <summary>
        /// Berechnet den aktuellen Portfolio-Wert
        /// </summary>
        /// <returns></returns>
        void CalculateCurrentPortfolioValue();


    }
}