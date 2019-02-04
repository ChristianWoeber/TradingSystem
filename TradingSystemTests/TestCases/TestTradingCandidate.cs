using System;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Models;

namespace TradingSystemTests.TestCases
{
    public class TestTradingCandidate : ITradingCandidate
    {
        public TestTradingCandidate(bool isInvested, TransactionType type = TransactionType.Unknown)
        {
            IsInvested = isInvested;
            TransactionType = type;
        }

        public TestTradingCandidate(bool isInvested, TransactionType type, TestQuote testQuote)
        {
            IsInvested = isInvested;
            TransactionType = type;
            Record = testQuote;
        }

        public TestTradingCandidate(IScoringResult scoringResult, ITradingRecord record)
        {
            ScoringResult = scoringResult;
            Record = record;
        }

        public DateTime PortfolioAsof { get; set; }
        public ITransaction LastTransaction { get; set; }
        public ITransaction CurrentPosition { get; set; }
        public IScoringResult ScoringResult { get; }
        public ITradingRecord Record { get; }
        public bool IsInvested { get; }
        public bool IsTemporary { get; set; }
        public bool HasStopp { get; set; }
        public IScoringResult LastScoringResult { get; set; }
        public decimal TargetWeight { get; set; }
        public decimal CurrentWeight { get; set; }
        public decimal AveragePrice { get; }
        public TransactionType TransactionType { get; set; }
        public bool IsBelowStopp { get; set; }
        public bool HasBetterScoring { get; }
        public IRebalanceScoringResult RebalanceScore { get; }
        public decimal CalculatedScore { get; }

        public bool IsTemporarySell => throw new NotImplementedException();

        public decimal Performance => throw new NotImplementedException();
    }
}