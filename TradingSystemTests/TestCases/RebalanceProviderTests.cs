using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HelperLibrary.Database.Models;
using HelperLibrary.Parsing;
using HelperLibrary.Trading;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Util.Atrributes;
using NUnit.Framework;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Helper;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class RebalanceProviderTests
    {
        private IAdjustmentProvider _adjustmentProvider;
        private PortfolioManager _dummyPortfolioManager;
        private ICashManager _cashHandler;
        private RebalanceProvider _rebalanceProvider;

        public RebalanceProviderTests()
        {
            Init();
        }

        private void Init()
        {
            _dummyPortfolioManager = new PortfolioManager(null,
                new DummyPortfolioSettings(),
                new TransactionsHandler(new DummyTransactionsCacheprovider()));

            _cashHandler = _dummyPortfolioManager.CashHandler;
            _adjustmentProvider = _dummyPortfolioManager;
            _rebalanceProvider = new RebalanceProvider(_dummyPortfolioManager.TemporaryPortfolio, _adjustmentProvider, _dummyPortfolioManager.PortfolioSettings);
        }

        [TestCase("BestCandidatesNotInvestedIn_05.01.2000.txt", "Candidates_05.01.2000.txt", 1)]
        public void RebalanceTemporaryPortfolioTest(string bestCandidatesfileName, string candidatesFileName, int temporaryItemsIdx, decimal? portfolioValue = null)
        {
            //dummy Items hinzufügen
            _dummyPortfolioManager.PortfolioValue = portfolioValue ?? _cashHandler.Cash;
            _dummyPortfolioManager.TemporaryPortfolio.AddRange(GetTemporaryItems(temporaryItemsIdx));

            var bestCandidates = TestHelper.GetTestCollectionFromJson<List<TradingCandidate>>(bestCandidatesfileName);
            var candidates = TestHelper.GetTestCollectionFromJson<List<TradingCandidate>>(candidatesFileName);

            _rebalanceProvider.RebalanceTemporaryPortfolio(bestCandidates.Select(c => (ITradingCandidate)c).ToList(), candidates.Select(c => (ITradingCandidate)c).ToList());

            var dateTimeGroup = _dummyPortfolioManager.TemporaryPortfolio.GroupBy(x => x.TransactionDateTime);

            foreach (var dateGrp in dateTimeGroup)
            {
                foreach (var secIdGroup in dateGrp.GroupBy(x => x.SecurityId))
                {
                    Assert.IsFalse(secIdGroup.Count(c => c.Cancelled == 0) > 1, "Achtung es gibt doppelte SecurityIds pro Datum");
                }
            }

            Assert.IsTrue(_dummyPortfolioManager.CashHandler.Cash > 0, "Achtung beim Rebalancing ist etwas schief gegangen");
            Assert.IsFalse(_dummyPortfolioManager.CurrentAllocationToRisk > _dummyPortfolioManager.PortfolioSettings.MaximumAllocationToRisk, "Achtung die Aktienquote ist zu hoch");
        }


        public List<ITransaction> GetTemporaryItems(int idx)
        {
            switch (idx)
            {
                case 1:
                    return SimpleTextParser.GetListOfType<Transaction>(
                        "TransactionDateTime;SecurityId;Shares;TargetAmountEur;TransactionType;Cancelled;TargetWeight;EffectiveWeight;EffectiveAmountEur" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 39; 343; 10000.0; 1; 0; 0.1; 0.0997; 9971.01" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 16; 253; 10000.0; 1; 0; 0.1; 0.0999; 9994.2586" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 40; 304; 10000.0; 1; 0; 0.1; 0.1000; 9996.5232" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 11; 153; 10000.0; 1; 0; 0.1; 0.0998; 9983.25" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 14; 497; 10000.0; 1; 0; 0.1; 0.1000; 9996.658" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 1; 131; 10000.0; 1; 0; 0.1; 0.1000; 9999.6230" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 23; 128; 10000.0; 1; 0; 0.1; 0.0994; 9943.0400" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 27; 300; 10000.0; 1; 0; 0.1; 0.0998; 9984.6004" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 9; 41; 10000.0; 1; 0; 0.1; 0.0998; 9982.8437" + $"{Environment.NewLine}" +
                        "05.01.2000 00:00:00; 7; 124; 10000.0; 1; 0; 0.1; 0.0995; 9951.00").Select(x => (ITransaction)x).ToList();
                case 2:
                default:
                    break;
            }

            return null;
        }

    }


    [TestFixture()]
    public class AdjustmentProviderTests
    {

    }


    public class TradingCandidateTest : ITradingCandidate
    {
        [InputMapping(KeyWords = new[] { nameof(PortfolioAsof) })]
        public DateTime PortfolioAsof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(LastTransaction) })]
        public ITransaction LastTransaction { get; set; }

        [InputMapping(KeyWords = new[] { nameof(CurrentPosition) })]
        public ITransaction CurrentPosition { get; set; }

        [InputMapping(KeyWords = new[] { nameof(ScoringResult) })]
        public IScoringResult ScoringResult { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Record) })]
        public ITradingRecord Record { get; set; }

        [InputMapping(KeyWords = new[] { nameof(IsInvested) })]
        public bool IsInvested { get; set; }

        [InputMapping(KeyWords = new[] { nameof(IsTemporary) })]
        public bool IsTemporary { get; set; }

        [InputMapping(KeyWords = new[] { nameof(HasStopp) })]
        public bool HasStopp { get; set; }

        [InputMapping(KeyWords = new[] { nameof(LastScoringResult) })]
        public IScoringResult LastScoringResult { get; set; }

        [InputMapping(KeyWords = new[] { nameof(TargetWeight) })]
        public decimal TargetWeight { get; set; }

        [InputMapping(KeyWords = new[] { nameof(CurrentWeight) })]
        public decimal CurrentWeight { get; set; }

        [InputMapping(KeyWords = new[] { nameof(AveragePrice) })]
        public decimal AveragePrice { get; set; }

        [InputMapping(KeyWords = new[] { nameof(TransactionType) })]
        public TransactionType TransactionType { get; set; }

        [InputMapping(KeyWords = new[] { nameof(IsBelowStopp) })]
        public bool IsBelowStopp { get; set; }

        [InputMapping(KeyWords = new[] { nameof(HasBetterScoring) })]
        public bool HasBetterScoring { get; set; }

        [InputMapping(KeyWords = new[] { nameof(SecurityId) })]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Name) })]
        public string Name { get; set; }


    }
}
