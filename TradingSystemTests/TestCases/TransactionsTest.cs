using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using HelperLibrary.Database.Models;
using HelperLibrary.Extensions;
using HelperLibrary.Interfaces;
using HelperLibrary.Trading;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Trading.PortfolioManager.Transactions;
using NUnit.Framework;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Helper;
using TradingSystemTests.Models;

namespace TradingSystemTests.TestCases
{
    //TODO: Testfälle implementieren
    [TestFixture]
    public class CalculationHandlerTests
    {
        private TransactionCalculationHandler _handler;

        [SetUp]
        public void CreateTestCandidates()
        {
            if (_handler != null)
                return;

            var portfolioValuation = new PortfolioValuation { AllocationToRisk = 1, PortfolioAsof = DateTime.Today, PortfolioValue = new decimal(100000) };
            _handler = new TransactionCalculationHandler(portfolioValuation);
        }
        //Change TEST decrement
        [TestCase(true, 0.165, TransactionType.Changed, ExpectedResult = -16500)]
        //Closing TEST
        [TestCase(true, 0, TransactionType.Close, ExpectedResult = -33000)]
        //Opening TEST
        [TestCase(false, 0.15, TransactionType.Open, ExpectedResult = 15000)]
        public decimal? CalculateTargetAmountTest(bool isInvested, decimal targetWeight, TransactionType type)
        {
            var testCandidate = new TestTradingCandidate(isInvested, type, new TestQuote { AdjustedPrice = 100 }) { TargetWeight = targetWeight };
            if (type != TransactionType.Close && type != TransactionType.Changed)
                return _handler.CalculateTargetAmount(testCandidate);

            testCandidate.LastTransaction = CreateTransaction();
            testCandidate.CurrentPosition = CreateTransaction();

            return _handler.CalculateTargetAmount(testCandidate);
        }

        private ITransaction CreateTransaction()
        {
            return new Transaction
            {
                Cancelled = 0,
                EffectiveAmountEur = 14995,
                EffectiveWeight = (decimal)0.1499,
                Shares = 330,
                TargetWeight = (decimal)0.33,
                TargetAmountEur = 33000,
            };

        }

        [TestCase(true, 1.5, TransactionType.Open)]
        [TestCase(true, -0.5, TransactionType.Changed)]
        [TestCase(true, 0, TransactionType.Changed)]
        [TestCase(false, 0, TransactionType.Unknown)]
        public void CalculateTargetAmountTestThrows(bool isInvested, decimal targetWeight, TransactionType type)
        {
            Assert.That(() => CalculateTargetAmountTest(isInvested, targetWeight, type), Throws.ArgumentException);
        }

        //Change TEST decrement
        [TestCase(true, -16500, TransactionType.Changed, ExpectedResult = -165)]
        //Closing TEST
        [TestCase(true, -33000, TransactionType.Close, ExpectedResult = -330)]
        //Opening TEST
        [TestCase(false, 15000, TransactionType.Open, ExpectedResult = 150)]
        public int CalculateSharesTest(bool isInvested, decimal targetAmount, TransactionType type)
        {
            var testCandidate = new TestTradingCandidate(isInvested, type, new TestQuote { AdjustedPrice = 100, Asof = DateTime.Today, Price = 100 });
            if (isInvested)
                testCandidate.CurrentPosition = CreateTransaction();
            return _handler.CalculateTargetShares(testCandidate, targetAmount);
        }


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
        }
    }

    [TestFixture]
    public class TransactionsTest
    {
        private Dictionary<int, IPriceHistoryCollection> _priceHistoryCollection;

        [SetUp]
        public void InitTestCollection()
        {
            if (_priceHistoryCollection == null)
                _priceHistoryCollection = TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx");
        }

        [TestCase("GetCurrentTransactionsTest")]
        public void GetCurrentTransactionsTest(string filename)
        {
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions));

            Assert.IsTrue(pm.CurrentPortfolio.Count() == 11);
        }

        [TestCase("GetCurrentTransactionsTest")]
        public void GetScoringfromCurrentTransactionsTest(string filename)
        {
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions));
            var scoreProvider = new ScoringProvider(_priceHistoryCollection);
            pm.RegisterScoringProvider(scoreProvider);

            foreach (var position in pm.CurrentPortfolio)
            {
                var score = scoreProvider.GetScore(position.SecurityId, position.TransactionDateTime);
                Assert.IsTrue(score != null);
            }
        }


        [TestCase("TransactionsHistoryTest", "17.10.2017", 1)]
        public void TransactionsHistoryTest(string filename, string asof, int testSecid)
        {
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(new TransactionsCacheProviderTest(() => LoadHistory(filename))));

            var weight = pm.TransactionsHandler.GetWeight(testSecid, DateTime.Parse(asof));

            Assert.IsTrue(weight.Value == new decimal(0.1));
        }

        private Dictionary<int, List<ITransaction>> LoadHistory(string filename)
        {
            return TestHelper.CreateTestCollection<Transaction>(filename)?.Cast<ITransaction>().ToDictionaryList(x => x.SecurityId);
        }


        /// <summary>
        /// Testet die Get Methode des Transactionhandlers
        /// </summary>
        /// <param name="filename">der Name des testfiles</param>
        /// <param name="secId">die Securityid</param>
        /// <param name="type">der TransactionsType <see cref="TransactionType"/></param>
        /// <param name="getlatest">bool flag das angibt ob das letzte item zurückgegeben werden soll</param>
        [TestCase("GetTransactionTest", 10, TransactionType.Open)]
        [TestCase("GetTransactionTest", 9, TransactionType.Close)]
        [TestCase("GetTransactionTest", 10, TransactionType.Changed)]
        [TestCase("GetTransactionTest", 10, TransactionType.Changed, false)]
        [TestCase("GetTransactionTest", 10, null)]
        public void GetTransactionItemTest(string filename, int secId, TransactionType? type, bool? getlatest = true)
        {
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions, new TransactionsCacheProviderTest(() => LoadHistory(filename))));
            var item = pm.TransactionsHandler.GetSingle(secId, type, getlatest ?? true);

            if (type == null)
            {
                Assert.IsTrue(item.TransactionDateTime == DateTime.Parse("25.10.2017"));
                return;
            }

            switch (type)
            {
                case TransactionType.Open:
                    Assert.IsTrue(item.TransactionDateTime == DateTime.Parse("01.10.2017"));
                    break;
                case TransactionType.Close:
                    Assert.IsTrue(item.TransactionDateTime == DateTime.Parse("08.10.2017"));
                    break;
                case TransactionType.Changed:
                    if (getlatest == true)
                        Assert.IsTrue(item.TransactionDateTime == DateTime.Parse("25.10.2017"));
                    else
                        Assert.IsTrue(item.TransactionDateTime == DateTime.Parse("04.10.2017"));
                    break;
            }
        }

        [TestCase("GetCurrentTransactionsTest", 15, "04.10.2017")]
        public void HasCashTest(string filename, int secId, string asof)
        {
            var tesQuote = new TestQuote
            {
                Price = 100,
                AdjustedPrice = 100,
                Asof = new DateTime(2017, 10, 30),
                SecurityId = secId
            };
            var testScoringResult = new ScoringResult
            {
                Performance10 = (decimal)0.30,
                Performance30 = (decimal)0.39,
                Performance90 = (decimal)0.50,
                Performance250 = (decimal)0.55
            };

            var date = DateTime.Parse(asof);
            //TestCandidate erstellen
            var testCandidate = new Candidate(tesQuote, testScoringResult);
            //transaktionen erstellen
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            //pm erstellen
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions));
            //scoringprovider registrieren
            pm.RegisterScoringProvider(new ScoringProvider(_priceHistoryCollection));
            //testcandidaten übergeben
            pm.PassInCandidates(new List<ITradingCandidateBase> { testCandidate }, date);

            Assert.IsTrue(pm.CurrentPortfolio.Count() == 11, "in der Has Cash Methode stimmt etwas nicht");
        }


        [TestCase("GetCurrentTransactionsTest", 1)]
        public void IsActiveInvestmentTest(string filename, int secId)
        {
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions));
            var isActive = pm.TransactionsHandler.IsActiveInvestment(secId);

            Assert.IsTrue(isActive);
        }

        [TestCase("GetCurrentTransactionsTest", 10)]
        public void GetCurrentWeightTest(string filename, int secid)
        {
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions));
            var weight = pm.TransactionsHandler.GetWeight(secid);

            Assert.IsTrue(weight == (decimal)0.05);
        }

        /// <summary>
        /// ohne Asof gibt er mir den Durchschnittspreis aller Transaktionen zurück, ist fraglich ob das so gewollt ist...
        /// </summary>
        /// <param name="filename">Der Filename</param>
        /// <param name="asof">Das Datum</param>
        /// <param name="secId">Die Test SecurityId</param>
        [TestCase("GetCurrentTransactionsTest", "04.10.2017", 10)]
        public void GetCurrentPriceTest(string filename, string asof, int secId)
        {
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions, new TransactionsCacheProviderTest(() => LoadHistory(filename))));

            var price = pm.TransactionsHandler.GetPrice(secId, DateTime.Parse(asof));
            Assert.IsTrue(price == 800);
        }


        /// <summary>
        /// ohne Asof gibt er mir den Durchschnittspreis aller Transaktionen zurück, ist fraglich ob das so gewollt ist...
        /// </summary>
        /// <param name="filename">Der Filename</param>
        /// <param name="asof">Das Datum</param>
        /// <param name="secId">Die Test SecurityId</param>
        [TestCase("GetCurrentTransactionsTest", "04.10.2017", 10)]
        public void GetAveragePriceTest(string filename, string asof, int secId)
        {
            var transactions = TestHelper.CreateTestCollection<Transaction>(filename);
            var pm = new PortfolioManager(null, null, new TestTransactionsHandler(transactions, new TransactionsCacheProviderTest(() => LoadHistory(filename))));
            pm.RegisterScoringProvider(new ScoringProvider(_priceHistoryCollection));

            var averageprice = pm.TransactionsHandler.GetAveragePrice(secId, DateTime.Parse(asof));
            var price = pm.TransactionsHandler.GetPrice(secId, DateTime.Parse(asof));

            Assert.IsTrue(price == 800);
        }


    }
}
