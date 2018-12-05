using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing.Rules
{
    public class AllCandidatesAreUnchangedRule : INeeedRebalanceRule
    {
        public bool Apply(IEnumerable<ITradingCandidate> candidates)
        {
            return candidates.All(x => x.TransactionType != TransactionType.Unchanged);
        }

        public int SortIndex { get; set; } = 2;

        public IRebalanceContext Context { get; set; }
    }
}
