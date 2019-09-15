using System;
using NUnit.Framework;
using Trading.Core.Models;
using Trading.Core.Settings;
using Trading.Core.Transactions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Models;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class CalculationHandlerTests
    {
        private TransactionCalculationHandler _handler;
        private PortfolioValuation _portfolioValuation;

        [SetUp]
        public void CreateTestCandidates()
        {
            if (_handler != null)
                return;
            _portfolioValuation = new PortfolioValuation { AllocationToRisk = 1, PortfolioAsof = DateTime.Today, PortfolioValue = new decimal(100000) };
            _handler = new TransactionCalculationHandler(_portfolioValuation, new ConservativePortfolioSettings());
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

            _handler = new TransactionCalculationHandler(_portfolioValuation, new ConservativePortfolioSettings(){ExpectedTicketFee = 0});
            return _handler.CalculateTargetShares(testCandidate, targetAmount);
        }

        //Change TEST decrement
        //[TestCase(true, -16500, TransactionType.Changed, ExpectedResult = -165)]
        //////Closing TEST
        //[TestCase(true, -33000, TransactionType.Close, ExpectedResult = -330)]
        ////Opening TEST
        [TestCase(false, 15000, TransactionType.Open)]
        public void CreateTransactionTest(bool isInvested, decimal targetAmount, TransactionType type)
        {
            var testCandidate = new TestTradingCandidate(isInvested, type, new TestQuote { AdjustedPrice = 100, Asof = DateTime.Today, Price = 100 });
            if (isInvested)
                testCandidate.CurrentPosition = CreateTransaction();
            else
            {
                testCandidate.TargetWeight = 0.05M;
            }
            var transaction = _handler.CreateTransaction(testCandidate, targetAmount);

            Assert.IsTrue(transaction != null);
        }

    }
}