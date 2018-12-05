using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing.Rules
{
    /// <summary>
    /// Wenn ich in den ersten(=besten) Kandidat nicht investiert bin muss ich in jedem Fall Rebalancen
    /// </summary>
    public class FirstCandidateIsNotInvesterdInRule : INeeedRebalanceRule
    {
        public bool Apply(IEnumerable<ITradingCandidate> candidates)
        {
            return candidates.FirstOrDefault()?.IsInvested != true;
        }

        public int SortIndex { get; set; } = 1;

        public IRebalanceContext Context { get; set; }
    }
}