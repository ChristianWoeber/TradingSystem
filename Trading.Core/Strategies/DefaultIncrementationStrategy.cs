﻿using Trading.DataStructures.Interfaces;

namespace Trading.Core.Strategies
{
    /// <summary>
    /// die Strategie die für das Aufstocken einer Position herangezogen wird
    /// </summary>
    public class DefaultIncrementationStrategy : IPositionIncrementationStrategy
    {
        private readonly ITradingCandidate _candidate;
        private readonly IAdjustmentProvider _adjustmentProvider;
        private readonly IPortfolioSettings _settings;

        public DefaultIncrementationStrategy(ITradingCandidate candidate, IAdjustmentProvider adjustmentProvider,
            IPortfolioSettings settings)
        {
            _candidate = candidate;
            _adjustmentProvider = adjustmentProvider;
            _settings = settings;
        }

        /// <summary>
        /// gibt an ob ein Kandidate aufgestock werden darf
        /// </summary>
        public bool IsAllowedToBeIncremented()
        {
            //gibt an ob sich die Position unter den Top 5 Performern, gemessen am Total Return des Position, befindet
            //wenn ja darf sie wenn sich sich aktuell an einem neuen High befindet aufgestockt werden
            var isUnderTopPositions = _adjustmentProvider.PositionWatcher.IsUnderTopPositions(_candidate.Record.SecurityId, (int)(1/_settings.MaximumPositionSize));
            var meta = _adjustmentProvider.PositionWatcher.GetStopLossMeta(_candidate);
            return _candidate.Record.AdjustedPrice >= meta?.High.Price && isUnderTopPositions;
        }
    }
}