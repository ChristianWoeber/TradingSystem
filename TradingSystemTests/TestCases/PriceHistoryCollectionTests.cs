using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HelperLibrary.Collections;
using HelperLibrary.Extensions;
using NUnit.Framework;
using Trading.DataStructures.Enums;
using TradingSystemTests.Models;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class PriceHistoryCollectionTests
    {
        private PriceHistoryCollection _history;
        private const double TOLERANCE = 0.01;

        [TestCase("AdidasHistory.txt")]
        public void CreatePriceHistoryTest(string fileName)
        {
            var data = CreateTestCollecton(fileName);
            _history = new PriceHistoryCollection(data);

            Assert.IsTrue(_history != null, "Achtung die Collection ist null");
            Assert.IsTrue(_history.Count > 100, "Achtung es konnten nicht alle daten geladen werden");
        }

        [TestCase("AdidasHistory.txt", "17.11.1995", "08.03.2018")]
        public void TestFirstAndLastItem(string filename, string firstDate, string currentDate)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);

            Assert.IsTrue(_history.FirstItem.Asof == DateTime.Parse(firstDate));
            Assert.IsTrue(_history.LastItem.Asof == DateTime.Parse(currentDate));
        }


        [TestCase("AdidasHistory.txt", 17.1024, 0.1385)]
        public void TestReturnCalculations(string filename, double compoundReturn, double anualizedReturn)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);

            var totalReturnCompound = _history.Calc.GetAbsoluteReturn(_history.FirstItem.Asof);
            var totalReturnAnualized = _history.Calc.GetAverageReturn(_history.FirstItem.Asof);

            Assert.IsTrue(Math.Abs(compoundReturn - (double)Math.Round(totalReturnCompound, 4)) < TOLERANCE);
            Assert.IsTrue(Math.Abs(anualizedReturn - (double)Math.Round(totalReturnAnualized, 4)) < TOLERANCE);
        }

        [TestCase("Daimler.txt", 0.3353)]
        [TestCase("AdidasHistory.txt", 0.2888)]
        public void TestVolatilityCalculation(string filename, double voltilityMonthly)
        {
            CreatePriceHistoryTest(filename);

            var volatility = _history.Calc.GetVolatilityMonthly(_history.FirstItem.Asof);

            Assert.IsTrue(Math.Abs(voltilityMonthly - (double)Math.Round(volatility, 4)) < 0.01, $"Achtung bei der Berechung der Voltilität ist ein Fehler aufgetreten: Soll-Wert: {voltilityMonthly:P}<> berechneter Wert{volatility:P}");
        }

        [TestCase("AdidasHistory.txt", -0.7154)]
        public void TestMaxDrawdownCalculation(string filename, double maxDrawdown)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);

            var maximumDrawdown = _history.Calc.GetMaximumDrawdown(_history.FirstItem.Asof);

            Assert.IsTrue(Math.Abs(maxDrawdown - (double)Math.Round(maximumDrawdown, 4)) < TOLERANCE);
        }

        [TestCase("AdidasHistory.txt", PriceHistoryOption.PreviousItem, "01.01.2010")]
        [TestCase("AdidasHistory.txt", PriceHistoryOption.NextItem, "01.01.2010")]
        public void PriceHistoryGetTest(string filename, PriceHistoryOption option, string asof)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);

            var date = DateTime.Parse(asof);

            switch (option)
            {
                case PriceHistoryOption.PreviousItem:
                    var itemPrevios = _history.Get(date, option);
                    var spanPrevious = (date - itemPrevios.Asof);
                    Assert.IsTrue(spanPrevious > TimeSpan.FromDays(1) && spanPrevious < TimeSpan.FromDays(5));
                    break;
                case PriceHistoryOption.NextItem:
                    var itemNext = _history.Get(date, option);
                    Assert.IsTrue(itemNext.Asof - date >= TimeSpan.FromDays(1) && itemNext.Asof - date < TimeSpan.FromDays(5));
                    break;
            }
        }

        [TestCase("AdidasHistoryMissingItems.txt", PriceHistoryOption.PreviousItem, "01.01.2010")]
        public void PriceHistoryGetNullTest(string filename, PriceHistoryOption option, string asof)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);

            var date = DateTime.Parse(asof);

            switch (option)
            {
                case PriceHistoryOption.PreviousItem:
                    var itemPrevios = _history.Get(date, option);
                    Assert.IsTrue(itemPrevios == null);
                    break;
            }
        }

        [TestCase("AdidasHistoryMissingItems.txt", "01.01.2009", "01.01.2010")]
        public void PriceHistoryGetRangeTest(string filename, string from, string to)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);

            var start = DateTime.Parse(from);
            var end = DateTime.Parse(to);

            var range = _history.Range(start, end);

            Assert.IsTrue(range != null && range.Count() > 150);
        }

        [TestCase("AdidasHistoryMissingItems.txt")]
        public void PriceHistoryEnumMonthlyUltimoTest(string filename)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);

            foreach (var itm in _history.EnumMonthlyUltimoItems())
                Assert.IsTrue(itm.Asof.IsBusinessDayUltimo() || itm.Asof.IsUltimo());
        }


        public static IEnumerable<TestQuote> CreateTestCollecton(string fileName)
        {
            var data = (string)Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(fileName) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + fileName));

            if (data == null)
                throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + fileName);

            var split = data.Split(Environment.NewLine.ToCharArray());

            foreach (var line in split)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var item = line.Split(';');
                var date = DateTime.Parse(item[0]);
                var price = Convert.ToDecimal(item[1]);

                yield return new TestQuote
                {
                    Asof = date,
                    Price = price,
                    AdjustedPrice = price,
                };
            }
        }
    }
}
