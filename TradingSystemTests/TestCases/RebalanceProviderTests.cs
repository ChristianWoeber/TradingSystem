using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HelperLibrary.Extensions;
using HelperLibrary.Trading;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Trading.PortfolioManager.Rebalancing;
using HelperLibrary.Trading.PortfolioManager.Settings;
using NUnit.Framework;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Helper;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class RebalanceProviderTests
    {
        private IAdjustmentProvider _adjustmentProvider;
        private ICashManager _cashHandler;
        private RebalanceProvider _rebalanceProvider;
        private PortfolioManager _portfolioManager;
        private ScoringProvider _scoringProvider;

        public RebalanceProviderTests()
        {

        }

        private void Init(ITradingCandidate candidate, DateTime? start = null, DateTime? end = null)
        {
            var startDate = start ?? new DateTime(1999, 01, 1);
            var endDate = end ?? new DateTime(2002, 12, 31);

            _portfolioManager = new PortfolioManager(
                null,
                new ConservativePortfolioSettings
                {
                    IndicesDirectory = @"D:\Work\Private\Git\HelperLibrary\TradingSystemTests\Resources"
                },
                GetHandler(candidate));
            _portfolioManager.TemporaryPortfolio.AddRange(_portfolioManager.CurrentPortfolio);

            _scoringProvider = new ScoringProvider(TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx", startDate, endDate));
            _portfolioManager.RegisterScoringProvider(_scoringProvider);

            var portfolioValuation = typeof(TradingCandidate).GetField("_valuation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (portfolioValuation?.GetValue(candidate) is IPortfolioValuation valuation)
            {
                _portfolioManager.PortfolioAsof = valuation.PortfolioAsof;
                _portfolioManager.PortfolioValue = valuation.PortfolioValue;
                _portfolioManager.AllocationToRisk = valuation.AllocationToRisk;
                _portfolioManager.CashHandler.Cash = valuation.PortfolioValue * (1 - valuation.AllocationToRisk);
            }

            _cashHandler = _portfolioManager.CashHandler;
            _adjustmentProvider = _portfolioManager;
            _rebalanceProvider = new RebalanceProvider(_portfolioManager.TemporaryPortfolio, _adjustmentProvider, _portfolioManager.PortfolioSettings);
        }

        private ITransactionsHandler GetHandler(ITradingCandidate candidate)
        {
            //hol mir aus dem serilaiiserten Backingfield den "echten" transactionsHandler
            var transactionsHandler = typeof(TradingCandidate).GetField("_transactionsHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            if (transactionsHandler?.GetValue(candidate) is ITransactionsHandler handler)
            {
                //aktuell buggt der noch und fügt beim deserialiseren immer 2 Mal die Items ein
                var duplicates = new List<ITransaction>();
                foreach (var grp in handler.CurrentPortfolio.GroupBy(t => t.UniqueKey))
                {
                    if (grp.Count() > 1)
                    {
                        var duplicate = grp.ToList()[1];
                        duplicates.Add(duplicate);
                    }
                }

                //daher die bereiningung hier
                if (duplicates.Count > 0)
                    duplicates.ForEach(d => handler.CurrentPortfolio.Remove(d));
                return handler;
            }

            return null;
        }

        [TestCase("AllCandidates_16.03.2000.txt")]
        public void RebalanceTemporaryPortfolioStopsTest(string candidatesFileName)
        {
            var candidates = TestHelper.CreateTestCollectionFromJson<List<TradingCandidate>>(candidatesFileName) ?? new List<TradingCandidate>();
            var date = candidatesFileName.Substring(candidatesFileName.IndexOf("_", StringComparison.Ordinal), 11).Replace("_", "");

            Init(candidates[0], null, DateTime.Parse(date));
            _rebalanceProvider.RebalanceTemporaryPortfolio(new List<ITradingCandidate>(), candidates.Select(c => (ITradingCandidate)c).ToList());

            var dateTimeGroup = _portfolioManager.TemporaryPortfolio.GroupBy(x => x.TransactionDateTime);
            foreach (var dateGrp in dateTimeGroup)
            {
                foreach (var secIdGroup in dateGrp.GroupBy(x => x.SecurityId))
                {
                    Assert.IsFalse(secIdGroup.Count(c => c.Cancelled == 0) > 1, "Achtung es gibt doppelte SecurityIds pro Datum");
                }
            }

            Assert.IsTrue(_portfolioManager.CashHandler.Cash > 0, "Achtung beim Rebalancing ist etwas schief gegangen");
        }


       
        [TestCase("BestCandidates_16.02.2000.txt", "AllCandidates_16.02.2000.txt")]
       
        public void RebalanceTemporaryPortfolioTest(string bestCandidatesfileName, string candidatesFileName)
        {
            var bestCandidates = TestHelper.CreateTestCollectionFromJson<List<ITradingCandidate>>(bestCandidatesfileName) ?? new List<ITradingCandidate>();
            var candidates = TestHelper.CreateTestCollectionFromJson<List<ITradingCandidate>>(candidatesFileName) ?? new List<ITradingCandidate>();

            var file = bestCandidatesfileName ?? candidatesFileName;
            var date = file.Substring(file.IndexOf("_", StringComparison.Ordinal), 11).Replace("_", "");

            Init(candidates[0], null, DateTime.Parse(date));
            _rebalanceProvider.RebalanceTemporaryPortfolio(bestCandidates, candidates);

            var dateTimeGroup = _portfolioManager.TemporaryPortfolio.GroupBy(x => x.TransactionDateTime);

            foreach (var dateGrp in dateTimeGroup)
            {
                foreach (var secIdGroup in dateGrp.GroupBy(x => x.SecurityId))
                {
                    Assert.IsFalse(secIdGroup.Count(c => c.Cancelled == 0) > 1, "Achtung es gibt doppelte SecurityIds pro Datum");
                }
            }

            var maxBoundary = _portfolioManager.PortfolioSettings.MaximumAllocationToRisk *
                              (1 - _portfolioManager.PortfolioSettings.AllocationToRiskBuffer);

            var minBoundryMinimum = _portfolioManager.PortfolioSettings.MinimumAllocationToRisk *
                                    (1 - _portfolioManager.PortfolioSettings.AllocationToRiskBuffer);
            var minBoundryMaximum = _portfolioManager.PortfolioSettings.MinimumAllocationToRisk *
                                    (1 + _portfolioManager.PortfolioSettings.AllocationToRiskBuffer);


            Assert.IsTrue(_portfolioManager.CashHandler.Cash > 0, "Achtung beim Rebalancing ist etwas schief gegangen");

            Assert.IsTrue(_portfolioManager.CurrentAllocationToRisk.IsBetween(maxBoundary, _portfolioManager.PortfolioSettings.MaximumAllocationToRisk), "Achtung die Aktienquote ist nicht in Range");
            if (_portfolioManager.PortfolioSettings.MaximumAllocationToRisk <= _portfolioManager.PortfolioSettings.MinimumAllocationToRisk)
                Assert.IsTrue(_portfolioManager.CurrentAllocationToRisk.IsBetween(minBoundryMinimum, minBoundryMaximum), "Achtung die Aktienquote ist nicht in Range");

        }

        [TestCase("BestCandidates_25.10.2000.txt", "AllCandidates_25.10.2000.txt", 0.0, 0.2)]
        [TestCase("BestCandidates_25.10.2000.txt", "AllCandidates_25.10.2000.txt", 0.0)]
        [TestCase("BestCandidates_25.10.2000.txt", "AllCandidates_25.10.2000.txt", 0.2)]
        public void RebalanceTemporaryPortfolioTestAllocationToRisk(string bestCandidatesfileName, string candidatesFileName, double maxAllocationToRisk, double? minimumAllocationToRisk = null)
        {
            var bestCandidates = TestHelper.CreateTestCollectionFromJson<List<TradingCandidate>>(bestCandidatesfileName) ?? new List<TradingCandidate>();
            var candidates = TestHelper.CreateTestCollectionFromJson<List<TradingCandidate>>(candidatesFileName) ?? new List<TradingCandidate>();
            var file = bestCandidatesfileName ?? candidatesFileName;
            var date = file.Substring(file.IndexOf("_", StringComparison.Ordinal), 11).Replace("_", "");

            Init(candidates[0], null, DateTime.Parse(date));

            //die settings überschreiben
            _portfolioManager.PortfolioSettings.MaximumAllocationToRisk =new decimal(maxAllocationToRisk);
            _portfolioManager.PortfolioSettings.MinimumAllocationToRisk = new decimal(minimumAllocationToRisk ?? 0);

            //TestMethode
            _rebalanceProvider.RebalanceTemporaryPortfolio(bestCandidates.Select(c => (ITradingCandidate)c).ToList(), candidates.Select(c => (ITradingCandidate)c).ToList());

            var dateTimeGroup = _portfolioManager.TemporaryPortfolio.GroupBy(x => x.TransactionDateTime);

            foreach (var dateGrp in dateTimeGroup)
            {
                foreach (var secIdGroup in dateGrp.GroupBy(x => x.SecurityId))
                {
                    Assert.IsFalse(secIdGroup.Count(c => c.Cancelled == 0) > 1, "Achtung es gibt doppelte SecurityIds pro Datum");
                }
            }

            var maxBoundary = _portfolioManager.PortfolioSettings.MaximumAllocationToRisk *
                              (1 - _portfolioManager.PortfolioSettings.AllocationToRiskBuffer);

            var minBoundryMinimum = _portfolioManager.PortfolioSettings.MinimumAllocationToRisk *
                                    (1 - _portfolioManager.PortfolioSettings.AllocationToRiskBuffer);
            var minBoundryMaximum = _portfolioManager.PortfolioSettings.MinimumAllocationToRisk *
                                    (1 + _portfolioManager.PortfolioSettings.AllocationToRiskBuffer);


            Assert.IsTrue(_portfolioManager.CashHandler.Cash > 0, "Achtung beim Rebalancing ist etwas schief gegangen");

            Assert.IsTrue(_portfolioManager.CurrentAllocationToRisk.IsBetween(maxBoundary, _portfolioManager.PortfolioSettings.MaximumAllocationToRisk), "Achtung die Aktienquote ist nicht in Range");
            if (_portfolioManager.PortfolioSettings.MaximumAllocationToRisk <= _portfolioManager.PortfolioSettings.MinimumAllocationToRisk)
                Assert.IsTrue(_portfolioManager.CurrentAllocationToRisk.IsBetween(minBoundryMinimum, minBoundryMaximum), "Achtung die Aktienquote ist nicht in Range");

        }

    }

    //TODO: Testfälle schreiben
    [TestFixture()]
    public class AdjustmentProviderTests
    {

    }

}
