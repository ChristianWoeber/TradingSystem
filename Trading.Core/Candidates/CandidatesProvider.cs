using System;
using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Candidates
{
    public class CandidatesProvider
    {
        private readonly IScoringProvider _scoringProvider;

        public CandidatesProvider(IScoringProvider scoring)
        {
            _scoringProvider = scoring;
        }


        public IEnumerable<ITradingCandidateBase> GetCandidates(DateTime inputDate, PriceHistoryOption option = PriceHistoryOption.PreviousItem)
        {
            //init Liste mit candidaten
            var listWithCandidates = new List<Candidate>();

            //ich itereriere die Collection von priceHistory
            foreach (var priceHistory in _scoringProvider.PriceHistoryStorage.Values)
            {
                if (priceHistory == null)
                {
                    throw new ArgumentException("PriceHistory darf nicht null sein! ");
                }

                //hole den record aus der PriceHistory
                var record = priceHistory.Get(inputDate, option);
                //Wenn der null ist gibt es ihn nicht und ich gehe zum nächsten
                if (record == null)
                    continue;

                //Namen setzen
                if (!string.IsNullOrWhiteSpace(priceHistory.Settings?.Name))
                    record.Name = priceHistory.Settings.Name;

                //den Score rechnen
                var score = _scoringProvider.GetScore(priceHistory.SecurityId, option == PriceHistoryOption.PreviousDayPrice ? record.Asof : inputDate);

                //wenn der score nicht valide ist den nächsten Kandidaten versuchen
                if (!score.IsValid)
                    continue;
                //adden
                listWithCandidates.Add(new Candidate(record, score));
            }

            return listWithCandidates.Count == 0
                ? null
                : listWithCandidates.OrderByDescending(x => x.ScoringResult);
        }
    }
}
