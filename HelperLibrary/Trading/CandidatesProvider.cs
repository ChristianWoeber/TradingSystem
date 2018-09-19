using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Collections;

namespace HelperLibrary.Trading
{
    public class CandidatesProvider
    {
        private readonly IScoringProvider _scoringProvider;

        public CandidatesProvider(IScoringProvider scoring)
        {
            _scoringProvider = scoring;
        }

        public IEnumerable<TradingCandidate> GetCandidates(DateTime startDateInput, PriceHistoryOption option = PriceHistoryOption.PreviousItem )
        {
            //init Liste mit candidaten
            var listWithCandidates = new List<TradingCandidate>();

            //ich itereriere die Collection von priceHistory
            foreach (var priceHistory in _scoringProvider.PriceHistoryStorage.Values)
            {
                var score = _scoringProvider.GetScore(priceHistory.SecurityId, startDateInput);

                if (!score.IsValid)
                    continue;

                //add candidate
                listWithCandidates.Add(new TradingCandidate(priceHistory.Get(startDateInput, option), score));
            }

            return listWithCandidates.Count == 0
                ? null
                : listWithCandidates.OrderByDescending(x => x.ScoringResult);
        }
    }
}
