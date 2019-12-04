using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Trading.Calculation.Collections;
using Trading.Calculation.Extensions;
using Trading.Core.Extensions;
using Trading.Core.Models;
using Trading.Core.Settings;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;
using TradingSystemTests.Models;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class SerializeSettingsTest
    {
        [TestCase]
        public void SerializePortfolioSettingsTest()
        {
            var settings = new DefaultPortfolioSettings();
            var json = JsonConvert.SerializeObject(settings);

            Assert.IsTrue(json != null, "Achtung das serializierte json ist null");
            Assert.IsTrue(JsonConvert.DeserializeObject<DefaultPortfolioSettings>(json) != null);
        }
    }

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

        [TestCase("AdidasHistory.txt", "01.01.1996", PriceHistoryOption.PreviousItem)]
        [TestCase("AdidasHistory.txt", "02.01.1996", PriceHistoryOption.PreviousDayPrice)]
        [TestCase("AdidasHistory.txt", "17.11.1995", PriceHistoryOption.NextItem)]
        public void GetItemTest(string filename, string stringdateTime, PriceHistoryOption option)
        {
            if (_history == null)
                CreatePriceHistoryTest(filename);
            var date = DateTime.Parse(stringdateTime);

            var item = _history.Get(date, option);
            switch (option)
            {
                case PriceHistoryOption.PreviousItem:
                    Assert.IsTrue(item.Asof == new DateTime(1995, 12, 29));
                    break;
                case PriceHistoryOption.NextItem:
                    Assert.IsTrue(item.Asof == new DateTime(1995, 11, 17));
                    break;
                case PriceHistoryOption.PreviousDayPrice:
                    Assert.IsTrue(item.Asof == new DateTime(1995, 12, 29));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, null);
            }
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

            Assert.IsTrue(Math.Abs(voltilityMonthly - (double)Math.Round(volatility, 4)) < 0.02, $"Achtung bei der Berechung der Voltilität ist ein Fehler aufgetreten: Soll-Wert: {voltilityMonthly:P}<> berechneter Wert {volatility:P}");
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

        /// <summary>
        /// Test Für eine History wo der  Erste Preis 1995 ist dann der nächste erst 1999
        /// </summary>
        /// <param name="dateString"></param>
        [TestCase("08.12.2016")]
        public void TryGetVolatilityTest(string dateString)
        {
            var data = SimpleTextParser.GetListOfType<TradingRecord>(Resource.EVOTEC_AG_430185);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetVolatilityInfo(DateTime.Parse(dateString), out var vola));
            Assert.IsTrue(vola.DailyVolatility > 0);
        }

        /// <summary>
        /// Test Für eine History wo der  Erste Preis 1995 ist dann der nächste erst 1999
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dateString"></param>
        [TestCase("ADVANCED_MICRO_DEVICES_404161", "27.03.2017")]
        public void TryGetVolatilityResultTest(string filename, string dateString)
        {
            var data = CreateTradingRecordFromFileName(filename);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetVolatilityInfo(DateTime.Parse(dateString), out var vola));
            Assert.IsTrue(vola.DailyVolatility > 0.6M && vola.DailyVolatility < 0.65M);

            Assert.IsTrue(_history.Calc.TryGetLastVolatility(DateTime.Parse(dateString), out var volaDecimal));
            Assert.IsTrue(volaDecimal > 0.6M && volaDecimal < 0.65M);
        }

        /// <summary>
        /// Test Für eine History wo der  Erste Preis 1995 ist dann der nächste erst 1999
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dateString"></param>
        [TestCase("ADVANCED_MICRO_DEVICES_404161", "27.05.2017")]
        [TestCase("ADVANCED_MICRO_DEVICES_404161", "18.06.2018")]
        [TestCase("ADVANCED_MICRO_DEVICES_404161", "05.05.2000")]
        public void TryGetLastLowInfoCountNewHighs(string filename, string dateString)
        {
            var data = CreateTradingRecordFromFileName(filename);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetLowMetaInfo(DateTime.Parse(dateString), out var lowMetaInfo));

            if (dateString.Contains("2000") || dateString.Contains("2018"))
                Assert.IsTrue(lowMetaInfo.NewHighsCollection.Count > 20);
            if (dateString.Contains("2017"))
                Assert.IsTrue(lowMetaInfo.NewHighsCollection.Count < 10);
        }


        /// <summary>
        /// Test Für eine History wo der  Erste Preis 1995 ist dann der nächste erst 1999
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dateString"></param>
        [TestCase("ADVANCED_MICRO_DEVICES_404161", "09.01.2008")]
        [TestCase("ADVANCED_MICRO_DEVICES_404161", "25.11.2008")]
        public void TryGetLastLowInfoHasNewLow(string filename, string dateString)
        {
            var data = CreateTradingRecordFromFileName(filename);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetLowMetaInfo(DateTime.Parse(dateString), out var lowMetaInfo));
            Assert.IsTrue(lowMetaInfo.HasNewLow);
        }

        /// <summary>
        /// Test Für eine History wo der  Erste Preis 1995 ist dann der nächste erst 1999
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dateString"></param>
        [TestCase("ADVANCED_MICRO_DEVICES_404161", "17.02.2017")]
        public void CountNewHighsAndLowTestGreaterThanZero(string filename, string dateString)
        {
            var data = CreateTradingRecordFromFileName(filename);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetLowMetaInfo(DateTime.Parse(dateString), out var lowMetaInfo));
            Assert.IsTrue(lowMetaInfo.NewHighsCollection.Count > 0);
        }

        [TestCase("UNIVERSAL_HEALTH_SERVICES_B_401758")]
        [TestCase("WIRECARD_AG_428924")]
        public void WireCardNoExceptionTest(string filename)
        {
            var data = CreateTradingRecordFromFileName(filename);
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetLowMetaInfo(_history.LastItem.Asof, out var lowMetaInfo));

        }

        [TestCase()]
        public void CountNewHighsAndLowTest()
        {
            var data = CreateTestRecordsFromCodeUpDownSymetric();
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetLowMetaInfo(_history.LastItem.Asof, out var lowMetaInfo));
            Assert.IsTrue(lowMetaInfo.NewHighsCollection.Count == 1);

            Assert.IsTrue(_history.TryGetLowMetaInfo(new DateTime(2019, 06, 1), out var lowMetaInfoPeak));
            Assert.IsTrue(lowMetaInfoPeak.NewHighsCollection.Count == 149);
        }

        /// <summary>
        /// Testet ob die Highs auch korrekt nachgezogen werden
        /// </summary>
        [TestCase]
        public void CountNewHighsAndLowRollingHighsTest()
        {
            var data = CreateTestRecordsFromCodeUpDownSteps();
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, new PriceHistoryCollectionSettings());

            Assert.IsTrue(_history.TryGetLowMetaInfo(new DateTime(2019, 06, 1), out var lowMetaInfo));

            var lastHigh = lowMetaInfo.High;

            Assert.IsTrue(_history.TryGetLowMetaInfo(new DateTime(2019, 10, 30), out var lowMetaInfoPeak));
            Assert.IsTrue(lowMetaInfoPeak.High.AdjustedPrice > lastHigh.AdjustedPrice);
            Assert.IsTrue(lowMetaInfoPeak.PositiveDailyRetunsMetaInfo.Count < 150);

        }

        /// <summary>
        /// Testet ob die der, Count der neuen Highs auch korrekt angepasst wird
        /// was passiert wenn ein Wertpapier 300 Tage in Fole steigen würde => Dann düfte der Count der New Highs auch nie mehr als die Länger der Periode sein z.b.: 150 Tage etc..
        /// </summary>
        [TestCase]
        public void CountNewHighsRollingTest()
        {
            var data = CreateTestRecordsFromCodeOnlyOneWay(PriceMovementType.Rising, 301);
            var settings = new PriceHistoryCollectionSettings();
            _history = (PriceHistoryCollection)PriceHistoryCollection.Create(data, settings);

            Assert.IsTrue(_history.TryGetLowMetaInfo(_history.LastItem.Asof, out var lowMetaInfo));
            Assert.IsTrue(lowMetaInfo.NewHighsCollection.Count == settings.MovingLowsLengthInDays);
            Assert.IsTrue(lowMetaInfo.PositiveDailyRetunsMetaInfo.Count == settings.MovingLowsLengthInDays-1);
        }


        private IEnumerable<ITradingRecord> CreateTestRecordsFromCodeUpDownSymetric()
        {
            ITradingRecord lastRecord = new TradingRecord() { Asof = new DateTime(2019, 01, 01), AdjustedPrice = 100, Price = 100, Name = "Test", SecurityId = 1 };

            for (var i = 0; i < 300; i++)
            {
                var record = i < 151
                    ? AdjustRecord(lastRecord, PriceMovementType.Rising)
                    : AdjustRecord(lastRecord, PriceMovementType.Sinking);
                yield return record;
                lastRecord = record;
            }
        }

        private IEnumerable<ITradingRecord> CreateTestRecordsFromCodeUpDownSteps()
        {
            ITradingRecord lastRecord = new TradingRecord() { Asof = new DateTime(2019, 01, 01), AdjustedPrice = 100, Price = 100, Name = "Test", SecurityId = 1 };

            var mode = PriceMovementType.Rising;

            for (var i = 1; i <= 901; i++)
            {
                if (i % 150 == 0)
                    mode = mode == PriceMovementType.Rising ? PriceMovementType.Sideway : PriceMovementType.Rising;

                var record = AdjustRecord(lastRecord, mode);
                yield return record;
                lastRecord = record;
            }
        }

        /// <summary>
        /// generiert mir Einträge in eine Richtung
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="countRecords">die Anzahl der Records die generiert werden soll</param>
        /// <returns></returns>
        private IEnumerable<ITradingRecord> CreateTestRecordsFromCodeOnlyOneWay(PriceMovementType mode, int countRecords = 601)
        {
            ITradingRecord lastRecord = new TradingRecord() { Asof = new DateTime(2019, 01, 01), AdjustedPrice = 100, Price = 100, Name = "Test", SecurityId = 1 };

            for (var i = 1; i <= countRecords; i++)
            {
                var record = AdjustRecord(lastRecord, mode);
                yield return record;
                lastRecord = record;
            }
        }
        private ITradingRecord AdjustRecord(ITradingRecord lastRecord, PriceMovementType mode)
        {
            var record = new TradingRecord(lastRecord) { Asof = lastRecord.Asof.AddDays(1) };

            switch (mode)
            {
                case PriceMovementType.Rising:
                    record.AdjustedPrice *= 1.01M;
                    record.Price *= 1.01M;
                    break;
                case PriceMovementType.Sinking:
                    record.AdjustedPrice *= 0.99M;
                    record.Price *= 0.99M;
                    break;
                case PriceMovementType.Sideway:
                    record.AdjustedPrice *= 1M;
                    record.Price *= 1M;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            return record;
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

        public static IEnumerable<TradingRecord> CreateTradingRecordFromFileName(string fileName)
        {
            var data = (string)Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(fileName) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + fileName));

            if (data == null)
                throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + fileName);

            return SimpleTextParser.GetListOfType<TradingRecord>(data);

        }

        public static IEnumerable<TestQuote> CreateTestCollectonFromParser(string fileName)
        {
            var data = (string)Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(fileName) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + fileName));

            if (data == null)
                throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + fileName);

            return SimpleTextParser.GetListOfType<TestQuote>(data);
        }



    }

    internal enum PriceMovementType
    {
        Rising,
        Sinking,
        Sideway
    }
}
