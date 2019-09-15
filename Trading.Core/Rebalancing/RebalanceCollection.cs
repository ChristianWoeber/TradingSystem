using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Rebalancing
{
    public class RebalanceCollection : IEnumerable<ITradingCandidate>
    {
        private readonly IPortfolioSettings _settings;
        private readonly List<ITradingCandidate> _items = new List<ITradingCandidate>();

        public RebalanceCollection(IPortfolioSettings settings)
        {
            _settings = settings;
        }

        public RebalanceCollection(IEnumerable<ITradingCandidate> candidates, IPortfolioSettings settings) : this(settings)
        {
            decimal sum = 0;
            foreach (var candidate in candidates.OrderByDescending(x => x.RebalanceScore.Score))
            {
                sum += candidate.TargetWeight;
                if (!candidate.IsInvested && sum <= 1)
                    RebalanceTargetWeight += candidate.TargetWeight;

                Add(candidate);
            }
        }

        public decimal RebalanceTargetWeight { get; set; }

        /// <summary>
        /// Der Count der Collection
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gibt mir an ob ich überhaupt etwas unternehemen muss
        /// </summary>     
        public bool NeedsRebalancing { get; set; }

        /// <summary>
        /// Gibt mir an ob es einen besseren Kandidaten ausserhalb des Portfolios gibt
        /// </summary>
        public bool HasBetterNotInvestedCandidate => BestNotInvestetdCandidate?.RebalanceScore.Score >
                                                     WorstInvestedCandidate?.RebalanceScore.Score * (1 + _settings.ReplaceBufferPct);

        /// <summary>
        /// Der beste aktuell nicht investierte Kandiat
        /// </summary>
        internal ITradingCandidate BestNotInvestetdCandidate => NotInvestedCandidates.FirstOrDefault();

        /// <summary>
        /// Der aschlechteste aktuell investierte Kandiat
        /// </summary>
        internal ITradingCandidate WorstInvestedCandidate => InvestedCandidates.LastOrDefault();

        /// <summary>
        /// Alle investierten Kandiaten
        /// </summary>
        internal IEnumerable<ITradingCandidate> InvestedCandidates => this.Where(x => x.IsInvested);

        /// <summary>
        /// Alle nicht investierten Kandiaten
        /// </summary>
        internal IEnumerable<ITradingCandidate> NotInvestedCandidates => this.Where(x => !x.IsInvested);



        public void Add(ITradingCandidate candidate)
        {
            _items.Add(candidate);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public IEnumerator<ITradingCandidate> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}