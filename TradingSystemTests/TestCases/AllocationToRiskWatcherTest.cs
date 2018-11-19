using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary.Extensions;
using HelperLibrary.Parsing;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Util.Atrributes;
using NUnit.Framework;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace TradingSystemTests.TestCases
{
    public class DummyTransactionsCacheprovider : ITransactionsCacheProvider
    {
        public Lazy<Dictionary<int, List<ITransaction>>> TransactionsCache { get; }
        public void UpdateCache()
        {
            throw new NotImplementedException();
        }
    }

    public class DummyPortfolioSettings : DefaultPortfolioSettings
    {
        public DummyPortfolioSettings()
        {
            IndicesDirectory = @"D:\Work\Private\Git\HelperLibrary\TradingSystemTests\Resources";
        }
    }


    public class IndexDummyExposureReceiver : IIndexBackTestResult
    {
        public IndexDummyExposureReceiver()
        {
            IndicesDirectory = @"D:\Work\Private\Git\HelperLibrary\TradingSystemTests\Resources";
        }

        public decimal MaximumAllocationToRisk { get; set; } = 1;
        public decimal MinimumAllocationToRisk { get; set; } /*= new decimal(0.20)*/
        public string IndicesDirectory { get; set; }
        public DateTime Asof { get; set; }
        public decimal SimulationNav { get; set; }
        public decimal IndexLevel { get; set; }
    }

    public class MovingAveragePrint
    {
        public MovingAveragePrint(DateTime asof, decimal movingAverage)
        {
            Asof = asof;
            MovingAverage = movingAverage;
        }

        [InputMapping(KeyWords = new[] { nameof(Asof) })]
        public DateTime Asof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(MovingAverage) })]
        public decimal MovingAverage { get; set; }

    }

    [TestFixture]
    public class AllocationToRiskWatcherTest
    {
        private static ExposureWatcher _allocationToRiskWatcher;
        private static IExposureSettings _settings;
        private static List<Tuple<DateTime, decimal>> _output;

        public static void Init(IExposureSettings settings)
        {
            if (_allocationToRiskWatcher != null)
                return;
            _settings = settings;
            _allocationToRiskWatcher = new ExposureWatcher(_settings, IndexType.MsciWorldEur);
            _output = new List<Tuple<DateTime, decimal>>();
        }
        [TestCase("01.01.2000", true)]
        [TestCase("01.09.2008", false)]
        public void CalculateMaximumExposureTest(string dateString, bool show)
        {
            Init(new DummyPortfolioSettings());
            if (show)
            {
                ShowFile("MovingAverageTest.csv");
            }

            var startDate = DateTime.Parse(dateString);
            var daysToAdd = 1;

            while (startDate < DateTime.Today)
            {
                _allocationToRiskWatcher.CalculateMaximumExposure(startDate);
                _output.Add(new Tuple<DateTime, decimal>(startDate, _settings.MaximumAllocationToRisk));

                if (startDate.DayOfWeek == DayOfWeek.Friday)
                    daysToAdd = 3;
                if (startDate.DayOfWeek == DayOfWeek.Saturday)
                    daysToAdd = 2;
                startDate = startDate.AddDays(daysToAdd);
            }
            ShowFile("ExposureTest1.csv");

            var dicList = _output.ToDictionaryList(x => x.Item2);
            var ret = dicList.TryGetValue(_settings.MinimumAllocationToRisk, out var minimumItems);

            Assert.IsTrue(ret, "Achtung keine Items mit mimum Exposre in der TimeRange gefunden");
            Assert.IsTrue(minimumItems.Count > 10, "Achtung keine Items mit mimum Exposre in der TimeRange gefunden");

        }


        [TestCase("01.01.2000", false)]
        public void CalculateIndexResultTest(string dateString, bool show)
        {
            Init(new IndexDummyExposureReceiver());
            if (show)
            {
                ShowFile("MovingAverageTest.csv");
            }

            var startDate = DateTime.Parse(dateString);
            var daysToAdd = 1;

            while (startDate < DateTime.Today)
            {
                _allocationToRiskWatcher.CalculateMaximumExposure(startDate);
                _allocationToRiskWatcher.CalculateIndexResult(startDate);
                _output.Add(new Tuple<DateTime, decimal>(startDate, ((IIndexBackTestResult)_settings).SimulationNav));

                if (startDate.DayOfWeek == DayOfWeek.Friday)
                    daysToAdd = 3;
                if (startDate.DayOfWeek == DayOfWeek.Saturday)
                    daysToAdd = 2;
                startDate = startDate.AddDays(daysToAdd);
            }
            ShowFile("IndexSimulation.csv");

            var dicList = _output.ToDictionaryList(x => x.Item2);
            var ret = dicList.TryGetValue(_settings.MinimumAllocationToRisk, out var minimumItems);

            Assert.IsTrue(!ret, "Achtung keine Items mit mimum Exposre in der TimeRange gefunden");
            Assert.IsTrue(minimumItems==null, "Achtung keine Items mit mimum Exposre in der TimeRange gefunden");

        }


        private static void ShowFile(string fileName)
        {
            var path = Path.GetTempPath();
            var completePath = Path.Combine(path, fileName);
            if (File.Exists(completePath))
                File.Delete(completePath);

            if (_output.Count == 0)
            {
                SimpleTextParser.AppendToFile(_allocationToRiskWatcher.EnumLows().Select(x => new MovingAveragePrint(x.Item1, x.Item2.MovingAverage)), Path.Combine(path, fileName));
                Process.Start(completePath);
            }
            else
            {
                SimpleTextParser.AppendToFile(_output.Select(x => new MovingAveragePrint(x.Item1, x.Item2)), Path.Combine(path, fileName));
                Process.Start(completePath);

            }
        }
    }
}
