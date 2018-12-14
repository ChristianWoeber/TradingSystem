using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Extensions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing.Rules
{
    public class ScanCandidatesRules : INeeedRebalanceRule
    {
        public bool Apply(IEnumerable<ITradingCandidate> candidates)
        {
            //versuche hier die Liste so weit zu scannen => ich gehe vom 2ten bis zum ersten nicht investierten Kandidaten
            //bzw bis ich nahe der maximalen investment quote bin
            var candidatesList = candidates.ToList();
            var currentSum = candidatesList[0].TargetWeight;
            for (var i = 1; i < candidatesList.Count; i++)
            {
                var currentCandidate = candidatesList[i];
                currentSum += currentCandidate.TargetWeight;

                if (currentSum.IsBetween(Context.MinimumBoundary, Context.MaximumBoundary) || currentSum >= Context.MaximumBoundary)
                {
                    if (candidatesList.GetRange(0, i+1).All(x => x.TransactionType == TransactionType.Unchanged))
                        return false;
                }

                if (!currentCandidate.IsInvested)
                {
                    if (candidatesList.GetRange(0, i+1).All(x => x.TransactionType == TransactionType.Unchanged))
                        return false;
                    break;
                }

            }
            return true;
        }

        public int SortIndex { get; set; } = 3;
        public IRebalanceContext Context { get; set; }

        /// <summary>
        /// Gibt an ob ich zum nächsten Regel weitergerhen kann
        /// default = treu
        /// </summary>
        public bool CanMoveNext { get; set; } = true;
    }
}