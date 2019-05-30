using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    public interface ISaveProvider
    {
        /// <summary>
        /// Methode um die Transaktionen zu speichern
        /// </summary>
        /// <param name="transactions"></param>
        void Save(IEnumerable<ITransaction> transactions);

        /// <summary>
        /// Methode um den Rebalance Score, swoie den Performance Score zu speichern und zu tracen
        /// </summary>
        /// <param name="portfolioManagerTemporaryCandidates"></param>
        /// <param name="portfolioManagerTemporaryPortfolio"></param>
        void SaveScoring(Dictionary<int, ITradingCandidate> portfolioManagerTemporaryCandidates, ITemporaryPortfolio portfolioManagerTemporaryPortfolio);
    }
}