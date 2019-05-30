using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Collections;
using Trading.DataStructures.Interfaces;
using Trading.DataStructures.Enums;

namespace HelperLibrary.Trading
{
    public class CandidatesProvider
    {
        private readonly IScoringProvider _scoringProvider;

        public CandidatesProvider(IScoringProvider scoring)
        {
            _scoringProvider = scoring;
        }

        public IEnumerable<ITradingCandidateBase> GetCandidates(DateTime inputDate, bool usePreviousDayScore = false, PriceHistoryOption option = PriceHistoryOption.PreviousItem)
        {
            //init Liste mit candidaten
            var listWithCandidates = new List<Candidate>();

            //ich itereriere die Collection von priceHistory
            foreach (var priceHistory in _scoringProvider.PriceHistoryStorage.Values)
            {
                if (priceHistory == null)
                {
                    continue;
                }

                //den Score rechnen
                var score = _scoringProvider.GetScore(priceHistory.SecurityId, inputDate);

                //wenn der score nicht valide ist den nächsten Kandidaten versuchen
                if (!score.IsValid)
                    continue;

                //Wenn der Score, der desselben Tages ist, muss ich für die Simulation den des Vortages nehmen
                if (usePreviousDayScore)
                {
                    if (score.Asof == inputDate)
                    {
                        inputDate = inputDate.AddDays(-1);
                        score = _scoringProvider.GetScore(priceHistory.SecurityId, inputDate);

                        //wenn der score nicht valide ist den nächsten Kandidaten versuchen
                        if (!score.IsValid)
                            continue;
                    }
                }

                //add candidate und Namen setzen
                var record = priceHistory.Get(inputDate, option);
                record.Name = priceHistory.Settings.Name;
                listWithCandidates.Add(new Candidate(record, score));
            }

            return listWithCandidates.Count == 0
                ? null
                : listWithCandidates.OrderByDescending(x => x.ScoringResult);
        }
    }
}
