using HelperLibrary.Database.Interfaces;
using HelperLibrary.Interfaces;

namespace HelperLibrary.Trading
{
    public class TradingCandidate
    {
        public ITradingRecord Record { get; }

        public IScoringResult ScoringResult { get; }

        public string TransactionKey => $"{Record.SecurityId}_{Record.Asof.Date}";

        public TradingCandidate(ITradingRecord record, IScoringResult scoring)
        {
            Record = record;
            ScoringResult = scoring;
        }

        public override string ToString()
        {
            return
                $"Name: {Record.Name} | Id: {Record.SecurityId} | Score: {ScoringResult.Score} | Asof: {ScoringResult.Asof} Price: {Record.AdjustedPrice}";
        }
    }
}
