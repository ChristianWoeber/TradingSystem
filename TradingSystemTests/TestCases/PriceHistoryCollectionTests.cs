using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HelperLibrary.Collections;
using HelperLibrary.Extensions;
using HelperLibrary.Parsing;
using HelperLibrary.Trading.PortfolioManager.Exposure;
using NUnit.Framework;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
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
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data);

            Assert.IsTrue(_history != null, "Achtung die Collection ist null");
            Assert.IsTrue(_history.Count > 100, "Achtung es konnten nicht alle daten geladen werden");
        }

        [TestCase("AdidasHistory.txt", 3, 5, 10, 15)]
        public void CalculateRollingPeriodsPriceHistoryTest(string fileName, params int[] periodesInYears)
        {
            var data = CreateTestCollecton(fileName);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data);

            _history.Calc.CreateRollingPeriodeResultsTask(periodesInYears).Wait();

            var histogramms = _history.Calc.EnumHistogrammClasses().ToList();

            foreach (var histogramm in histogramms)
            {
                foreach (var result in histogramm)
                {
                    Trace.TraceInformation($"Für das {result.PeriodeInYears} Jahres-Fenster lagen {result.RelativeFrequency:p2} " +
                                           $"der Daten im Bereich von {result.Minimum.Performance:p2} und {result.Maximum.Performance:p2}");
                }
            }

            Assert.IsTrue(histogramms.Count > 0);
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

        [TestCase("MICROCHIP_TECHNOLOGY.csv", "01.10.2001")]
        [TestCase("NVIDIA.csv", "01.10.2001")]
        [TestCase("AdidasHistory.txt", "21.11.2008")]
        public void CreatePriceHistoryWithCalculationSettingsTest(string fileName, string dateString)
        {
            var data = fileName.ContainsIc("Adidas") ? CreateTestCollecton(fileName) : CreateTestCollectonFromParser(fileName);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history != null, "Achtung die Collection ist null");
            Assert.IsTrue(_history.Count > 100, "Achtung es konnten nicht alle daten geladen werden");

            var date = DateTime.Parse(dateString);
            Assert.IsTrue(_history.TryGetLowMetaInfo(date, out var lowMetaInfo));
            Assert.IsTrue(_history.TryGetVolatilityInfo(date, out var volaMetaInfo));
            Assert.IsTrue(_history.TryGetAbsoluteLossesAndGains(date, out var absoluteLossesAndGainsMetaInfo));
            Assert.IsTrue(lowMetaInfo != null && lowMetaInfo.HasNewLow);
            Assert.IsTrue(volaMetaInfo != null && volaMetaInfo.DailyVolatility > 0);
            if (fileName.ContainsIc("Adidas"))
                Assert.IsTrue(absoluteLossesAndGainsMetaInfo != null && absoluteLossesAndGainsMetaInfo.AbsoluteSum < 0 && absoluteLossesAndGainsMetaInfo.AbsoluteLoss > -1);
            Assert.IsTrue(volaMetaInfo != null && volaMetaInfo.DailyVolatility > new decimal(0.49d));
        }




        public static IEnumerable<TestQuote> CreateTestCollecton(string fileName)
        {
            var data = (string)Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(fileName) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + fileName));

            if (data == null)
                throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + fileName);

            var split = data.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in split)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var item = line.Split(';');

                if (!DateTime.TryParse(item[0], out var date))
                    continue;

                var parsedDate = date;
                var price = Convert.ToDecimal(item[1]);

                yield return new TestQuote
                {
                    Asof = date,
                    Price = price,
                    AdjustedPrice = price,
                };
            }
        }


        public static IEnumerable<TestQuote> CreateTestCollectonFromParser(string fileName)
        {
            var data = (string)Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(fileName) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + fileName));

            if (data == null)
                throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + fileName);

            return SimpleTextParser.GetListOfType<TestQuote>(data);
        }


        //public class PriceHistoryCalculationSettings : IPriceHistoryCollectionSettings
        //{
        //    public PriceHistoryCalculationSettings(int movingdays = 150, int volaLength = 250)
        //    {
        //        MovingAverageLengthInDays = movingdays;
        //        MovingDaysVolatility = volaLength;
        //    }

        //    /// <summary>
        //    /// die Länge des Moving Averages
        //    /// </summary>
        //    public int MovingAverageLengthInDays { get; set; }

        //    /// <summary>
        //    /// die "Länge" der Volatilität
        //    /// </summary>
        //    public int MovingDaysVolatility { get; set; }
        //}
    }
}
