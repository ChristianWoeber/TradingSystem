using Trading.DataStructures.Interfaces;

namespace Trading.Core.Candidates
{
    public class Candidate : ITradingCandidateBase
    {
        public ITradingRecord Record { get; }
        public IScoringResult ScoringResult { get; }

        public Candidate()
        {
            
        }

        public Candidate(ITradingRecord record, IScoringResult scoringResult)
        {
            ScoringResult = scoringResult;
            Record = record;
        }

        public override string ToString()
        {
            return $"{Record.Name} Score:{ScoringResult.Score} Price:{Record.AdjustedPrice} Asof:{Record.Asof}";
        }
    }
}