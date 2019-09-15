using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Rebalancing.Rules
{
    public class AllCandidatesAreUnchangedRule : INeeedRebalanceRule
    {
        public bool Apply(IEnumerable<ITradingCandidate> candidates)
        {
            var allAreUnchanged = candidates.All(x => x.TransactionType == TransactionType.Unchanged);
            //Wenn alle nicht unchanged sind muss ich rebalancen
            if (!allAreUnchanged)
                return true;

            //ssont breche ich ab
            CanMoveNext = false;
            return false;
        }

        public int SortIndex { get; set; } = 2;

        public bool CanMoveNext { get; set; }

        public IRebalanceContext Context { get; set; }
    }
}
