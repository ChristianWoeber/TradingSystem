using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Interfaces;
using HelperLibrary.Trading;
using NUnit.Framework;
using TradingSystemTests.Helper;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class CandidatesTest
    {
        private Dictionary<int, IPriceHistoryCollection> _priceHistoryDictionary;

        [SetUp]
        public void Init()
        {
            if (_priceHistoryDictionary == null)
                _priceHistoryDictionary = TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx");
        }

        [TestCase("01.01.2010")]
        public void GetCandidatesTest(string asof)
        {
            var date = DateTime.Parse(asof);

            //einen BacktestHandler erstellen
            var backtestHandler = new CandidatesProvider(new ScoringProvider(_priceHistoryDictionary));

            //die Candidatenliste zrückgeben lassen
            var candidates = backtestHandler.GetCandidates(date);

            Assert.IsTrue(candidates.Any());
        }

        //Bei diesem Datum sollte es keinen positiven Score geben
        [TestCase("12.03.2003")]
        public void EmptyCandidatesTest(string asof)
        {
            var date = DateTime.Parse(asof);

            //einen BacktestHandler erstellen
            var backtestHandler = new CandidatesProvider(new ScoringProvider(_priceHistoryDictionary));

            //die Candidatenliste zurückgeben lassen
            var candidates = backtestHandler.GetCandidates(date);

            Assert.IsTrue(candidates == null);
        }
    }
}
