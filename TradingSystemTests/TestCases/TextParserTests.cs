using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HelperLibrary.Database.Models;
using HelperLibrary.Parsing;
using NUnit.Framework;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class TextParserTests
    {
        [TestCase(1000)]
        public void ParseFilesPerformanceTest(int iterations)
        {
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                var testdata = Resource.MSCIWorldEur;
                var records = SimpleTextParser.GetListOfType<TradingRecord>(testdata);
            }
            sw.Stop();
            Assert.IsTrue(sw.Elapsed.TotalSeconds > 1);
            Trace.TraceInformation($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");
        }

        [TestCase(1000, @"D:\Work\Private\Git\HelperLibrary\TradingSystemTests\Resources\MSCIWorldEur.csv")]
        public void ParseFilesPerformanceTestIEnumerable(int iterations, string path)
        {
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                //var testdata = Resource.MSCIWorldEur;
                var transactions = SimpleTextParser.GetItemsOfTypeFromFilePath<TradingRecord>(path).ToList();
            }
            sw.Stop();
            Assert.IsTrue(sw.Elapsed.TotalSeconds > 1);
            Trace.TraceInformation($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");
        }



        [TestCase(1000)]
        public void ParseFilesParallelPerformanceTest(int iterations)
        {
            var sw = new Stopwatch();
            sw.Start();
            //Parallel For Each ist in diesem Fall um den Faktor der kerne schneller
            Parallel.For(0, iterations, (state) =>
             {
                 var testdata = Resource.MSCIWorldEur;
                 var records = SimpleTextParser.GetListOfType<TradingRecord>(testdata);
             });
            sw.Stop();
            Assert.IsTrue(sw.Elapsed.TotalSeconds > 1);
            Trace.TraceInformation($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");
        }

    }
}