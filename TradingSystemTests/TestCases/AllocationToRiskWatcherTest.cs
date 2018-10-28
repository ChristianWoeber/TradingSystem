using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary.Trading.PortfolioManager;
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

    public class DummyExposureReceiver : IExposureReceiver
    {
        public decimal MaximumAllocationToRisk { get; set; } = 1;
        public decimal MinimumAllocationToRisk { get; set; } = new decimal(0.20);
    }

    [TestFixture]
    public class AllocationToRiskWatcherTest
    {
        private static ExposureWatcher _allocationToRiskWatcher;
        private static DummyExposureReceiver _dummyExposure;
        private static List<Tuple<DateTime, decimal>> _output;

        public static void Init()
        {
            if (_allocationToRiskWatcher != null)
                return;
            _dummyExposure = new DummyExposureReceiver();
            _allocationToRiskWatcher = new ExposureWatcher(_dummyExposure, new DummyPortfolioSettings(), ExposureWatcher.IndexType.MsciWorldEur);
            _output = new List<Tuple<DateTime,decimal>>();
        }

        //[TestCase("28.10.2008")]
        //[TestCase("21.10.2008")]
        //[TestCase("15.10.2008")]
        //[TestCase("08.10.2008")]
        //[TestCase("01.10.2008")]
        //[TestCase("28.09.2008")]
        //[TestCase("21.09.2008")]
        //[TestCase("15.09.2008")]
        //[TestCase("08.09.2008")]
        [TestCase("01.09.2008")]
        public void CreatePriceHistoryWithLowCalculationTest(string dateString)
        {
            Init();

            var startDate = DateTime.Parse(dateString);
            var daysToAdd = 1;

            while (startDate < new DateTime(2012, 01, 01))
            {
                _allocationToRiskWatcher.CalculateMaximumExposure(startDate);
                _output.Add(new Tuple<DateTime, decimal>(startDate,_dummyExposure.MaximumAllocationToRisk));

                if (startDate.DayOfWeek == DayOfWeek.Friday)
                    daysToAdd = 3;
                if (startDate.DayOfWeek == DayOfWeek.Saturday)
                    daysToAdd = 2;
                startDate = startDate.AddDays(daysToAdd);
            }

            foreach (var item in _output)
            {
                Trace.TraceInformation(item.Item1 +" "+item.Item2);
            }
        }
    }
}
