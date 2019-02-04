using System;
using System.Collections.Generic;
using HelperLibrary.Collections;
using HelperLibrary.Database.Models;
using HelperLibrary.Trading;
using HelperLibrary.Trading.PortfolioManager.Exposure;
using HelperLibrary.Trading.PortfolioManager.Settings;
using NUnit.Framework;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Helper;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class StoppLossServiceTests
    {
        public StoppLossServiceTests()
        {

        }

        [TestCase("SAFRAN", "08.12.1999", "10.12.1999")]
        [TestCase("SAFRAN", "24.11.1999", "16.03.2000")]
        public void HasStopLossTest(string fileName, string startDateString, string stopDateString)
        {
            var priceHistory = PriceHistoryCollection.Create(TestHelper.CreateTestCollection<TradingRecord>(fileName), new PriceHistoryCollectionSettings(60));
            //TODO : aus den Settings rausziehen
            var settings = new DefaultStopLossSettings();
            var scoringProvider = new ScoringProvider(
                new Dictionary<int, IPriceHistoryCollection>
                {
                    { priceHistory.SecurityId, priceHistory }
                });

            foreach (var record in priceHistory.Range(DateTime.Parse(startDateString), DateTime.Parse(stopDateString).AddDays(150)))
            {
                var score = scoringProvider.GetScore(record.SecurityId, record.Asof);
                settings.UpdateDailyLimits(new Transaction { SecurityId = record.SecurityId }, record.AdjustedPrice, record.Asof);

                if (!settings.HasStopLoss(new TestTradingCandidate(score, record)))
                    continue;

                Assert.IsTrue(DateTime.Parse(stopDateString) == record.Asof);
                return;
            }

            Assert.Fail("Achtung es konnte kein Stop ausgelöst werden");
        }
    }
}