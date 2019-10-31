using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Trading.Core.Models;
using Trading.Parsing;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class TextParserTests
    {
        [TestCase(1000)]
        public void ParseFilesPerformanceTest(int iterations)
        {
            var sw = new Stopwatch();
            var testdata = Resource.MSCIWorldEur;
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                var records = SimpleTextParser.GetListOfType<TradingRecord>(testdata);
            }
            sw.Stop();
            Assert.IsTrue(sw.Elapsed.TotalSeconds > 1);
            Trace.TraceInformation($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");
            Console.WriteLine($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");
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
            Console.WriteLine($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");

        }



        [TestCase(1000)]
        public void ParseFilesParallelPerformanceTest(int iterations)
        {
            var sw = new Stopwatch();
            sw.Start();
            //Parallel For Each ist in diesem Fall um den Faktor der kerne schneller
            Parallel.For(0, iterations, (i,state) =>
             {
                 var testdata = Resource.MSCIWorldEur;
                 var records = SimpleTextParser.GetListOfType<TradingRecord>(testdata);
                 if(i==iterations)
                     state.Stop();
             });
            sw.Stop();
            Assert.IsTrue(sw.Elapsed.TotalSeconds > 1);
            Trace.TraceInformation($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");
            Console.WriteLine($"Vergangene Zeit in Sekunden {sw.Elapsed.TotalSeconds} und in Minuten {sw.Elapsed.TotalMinutes}");
        }

        [TestCase(1000)]
        public void WriteToFileTest(int iterations)
        {
            var testdata = Resource.WriteToFileTest;
            var testPath = Path.Combine(Path.GetTempPath(), "TestTransactions.csv");
            if(File.Exists(testPath))
                File.Delete(testPath);

            var transactions = SimpleTextParser.GetListOfType<Transaction>(testdata);
            Assert.IsTrue(transactions.Count > 0 && transactions[0].TransactionDateTime > DateTime.MinValue);
            SimpleTextParser.AppendToFile<Transaction>(transactions, testPath);

            transactions = SimpleTextParser.GetListOfType<Transaction>(File.ReadAllText(testPath));
            Assert.IsTrue(transactions.Count > 0 && transactions[0].TransactionDateTime > DateTime.MinValue);
        }
    }
}