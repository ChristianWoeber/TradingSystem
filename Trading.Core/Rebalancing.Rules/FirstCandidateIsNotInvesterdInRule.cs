﻿using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Rebalancing.Rules
{
    /// <summary>
    /// Wenn ich in den ersten(=besten) Kandidat nicht investiert bin muss ich in jedem Fall Rebalancen
    /// </summary>
    public class FirstCandidateIsNotInvesterdInRule : INeeedRebalanceRule
    {
        /// <summary>
        /// Achtung Sortierreihenfolge beachten
        /// </summary>
        /// <param name="candidates"></param>
        /// <returns></returns>
        public bool Apply(IEnumerable<ITradingCandidate> candidates)
        {
            return candidates.FirstOrDefault()?.IsInvested != true;
        }

        public int SortIndex { get; set; } = 1;

        public IRebalanceContext Context { get; set; }

        /// <summary>
        /// Gibt an ob ich zum nächsten Regel weitergerhen kann
        /// default = treu
        /// </summary>
        public bool CanMoveNext { get; set; } = true;
    }
}