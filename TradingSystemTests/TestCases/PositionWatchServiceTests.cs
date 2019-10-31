using System;
using System.Collections.Generic;
using NUnit.Framework;
using Trading.Calculation.Collections;
using Trading.Core;
using Trading.Core.Models;
using Trading.Core.Scoring;
using Trading.Core.Settings;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Helper;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class PositionWatchServiceTests
    {
        public PositionWatchServiceTests()
        {

        }
        [TestCase("SAFRAN", "08.12.1999", "28.03.2000", "01.12.2000")]
        public void HasStopLossTest(string fileName, string startDateString, string highDateString, string stopDateString)
        {
            var priceHistory = PriceHistoryCollection.Create(TestHelper.CreateTestCollection<TradingRecord>(fileName), new PriceHistoryCollectionSettings(0));
  
            var settings = new DefaultStopLossSettings();
            var scoringProvider = new ScoringProvider(
                new Dictionary<int, IPriceHistoryCollection>
                {
                    { priceHistory.SecurityId, priceHistory }
                });

            var positionWatchService = new PositionWatchService(settings);
            var highDate = DateTime.Parse(highDateString);
            var endDate = DateTime.Parse(stopDateString);

            foreach (var record in priceHistory.Range(DateTime.Parse(startDateString), endDate.AddDays(10)))
            {
                var score = scoringProvider.GetScore(record.SecurityId, record.Asof);
                positionWatchService.UpdateDailyLimits(new Transaction { SecurityId = record.SecurityId }, record.AdjustedPrice, record.Asof);

                if (record.Asof == new DateTime(2000, 08, 03))
                {
                    var testCandidate = new TestTradingCandidate(score, record);
                    Assert.IsTrue(positionWatchService.HasStopLoss(testCandidate));
                }

                if (endDate == record.Asof)
                {
                    var testCandidate = new TestTradingCandidate(score, record);
                    var stoppLossMeta = positionWatchService.GetStopLossMeta(testCandidate);
                    Assert.IsTrue(stoppLossMeta.High.Asof == highDate);
                    Assert.IsTrue(positionWatchService.HasStopLoss(testCandidate));
                    return;
                }
            }

            Assert.Fail("Achtung es konnte kein Stop ausgelöst werden");
        }
    }
}