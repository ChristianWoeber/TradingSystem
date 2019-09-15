using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trading.Calculation.Collections;
using Trading.Core.Scoring;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Helper;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class ScoringTests
    {
        private PriceHistoryCollection _priceHistory;
        private Dictionary<int, IPriceHistoryCollection> _priceHistoryCollection;

        [SetUp]
        public void InitTestCollection()
        {
            _priceHistory = (PriceHistoryCollection)PriceHistoryCollection.Create(TestHelper.CreateTestCollection("AdidasHistory.txt", 1));
            _priceHistoryCollection = new Dictionary<int, IPriceHistoryCollection>
            {
                {_priceHistory.SecurityId, _priceHistory}
            };

        }

        [TestCase(1, "08.03.2016")]
        [TestCase(1, "01.01.2010")]
        public void TestScoringIsValid(int testSecurityId, string asof)
        {
            var date = DateTime.Parse(asof);
            var handler = new ScoringProvider(_priceHistoryCollection);
            var score = handler.GetScore(testSecurityId, date);

            Assert.IsTrue(score != null);
            Assert.IsTrue(score.IsValid);
        }

    }
}
