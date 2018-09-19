using HelperLibrary.Database.Interfaces;
using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
