using System.Collections.Generic;
using JetBrains.Annotations;

namespace Trading.DataStructures.Interfaces
{

    public interface IRebalanceProvider
    {
        /// <summary>
        /// die Methode zum Rebalancen des Portfolios - Hier werden die gravierenden Ausrichtungen vorgenommen und
        /// Maximale Aktienquote etc.. berücksichtigt
        /// </summary>
        /// <param name="bestCandidates">die besten nicht investierten Candidaten (Top 10)</param>
        /// <param name="allCandidates">alle Candidaten</param>
        void RebalanceTemporaryPortfolio([NotNull] List<ITradingCandidate> bestCandidates, [NotNull] List<ITradingCandidate> allCandidates);
    }
}
