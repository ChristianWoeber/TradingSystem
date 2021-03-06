﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using NLog;
using NUnit.Framework;
using Trading.Core.Backtest;
using Trading.Core.Candidates;
using Trading.Core.Cash;
using Trading.Core.Extensions;
using Trading.Core.Models;
using Trading.Core.Portfolio;
using Trading.Core.Scoring;
using Trading.Core.Transactions;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;
using TradingSystemTests.Helper;
using TradingSystemTests.Models;

namespace TradingSystemTests.TestCases
{
    [TestFixture]
    public class PortfolioManagerTest
    {
        private Dictionary<int, IPriceHistoryCollection> _priceHistoryDictionary;

        [SetUp]
        public void Init()
        {
            //if (_priceHistoryDictionary == null)
            //    _priceHistoryDictionary = TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx");

            //ConfigureLogger();

        }

        private static void ConfigureLogger(string namePortfolioValue = null, string nameCash = null)
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget(Path.GetFileNameWithoutExtension(namePortfolioValue)) { FileName = namePortfolioValue ?? "DEBUG_TEST.txt", ArchiveOldFileOnStartup = true, MaxArchiveFiles = 2, ArchiveAboveSize = 5000000 };
            var logfileCash = new NLog.Targets.FileTarget(Path.GetFileNameWithoutExtension(nameCash)) { FileName = nameCash ?? "DEBUG_TEST2.txt", ArchiveOldFileOnStartup = true, MaxArchiveFiles = 2, ArchiveAboveSize = 5000000 };

            //var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            //config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile, Path.GetFileNameWithoutExtension(namePortfolioValue));
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfileCash, Path.GetFileNameWithoutExtension(nameCash));

            LogManager.Configuration = config;
        }

        private Dictionary<int, List<ITransaction>> LoadHistory(string filename)
        {
            var trans = TestHelper.CreateTestCollection<Transaction>(filename).OfType<ITransaction>();
            return trans.ToDictionaryList(x => x.SecurityId);
        }


        /// <summary>
        /// Der Test erstellt einen Leeren PM und eröffnet die ersten Positionen
        /// Diese werden anschließend in ein File mit dem angegebenen filenamen geschrieben
        /// </summary>
        /// <param name="asof">Das Datum des Tests</param>
        /// <param name="filename">der Name des Files, dass als speicher dient</param>
        [TestCase("11.10.2016", "SetIntialTransactionsTest.csv")]
        public void CreateInitialPortfolioTest(string asof, string filename)
        {
            //datum parsen
            var date = DateTime.Parse(asof);

            //price history initialisieren
            if (_priceHistoryDictionary == null)
                _priceHistoryDictionary = TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx", date.AddDays(-250), date.AddDays(10));

            //Test Pm erstellen
            var pm = new PortfolioManager(null, null,
                new TransactionsHandler(new List<Transaction>(),
                new TransactionsCacheProviderTest(() => LoadHistory(filename))));

            //scoring provider erstellen
            var scoringProvider = new ScoringProvider(_priceHistoryDictionary);

            //scoring Provider registrieren
            pm.RegisterScoringProvider(scoringProvider);

            //einen BacktestHandler erstellen
            var backtestHandler = new CandidatesProvider(scoringProvider);

            //die Candidatenliste zurückgeben lassen
            var candidates = backtestHandler.GetCandidates(date);

            //dem Portfolio Manager die potentiellen Kandidaten übergeben
            pm.PassInCandidates(candidates.Where(x => x.ScoringResult.IsValid).ToList(), date);

            //Transaktionen speichern und im File anzeigen
            pm.TemporaryPortfolio.SaveTransactions(new TestSaveProvider(filename));

            //Nach initialer Erstellung des Portfolios müssen genau 10 Tansaktionen im Portfolio enthalten sein,
            Assert.IsTrue(pm.TemporaryPortfolio.Count == 10);

        }

        /// <summary>
        /// Der Test erstellt einen neuen PM und öffnet die beretis aus dem vorherigen test vorhandenen Positionen
        /// Diese werden anschließend wieder in das File geschrieben
        /// </summary>
        /// <param name="asof">Das Datum des Tests</param>
        /// <param name="filename">der Name des Files, dass als speicher dient</param>
        /// <param name="showInFile">Das Flag das angibt ob das File angezeigt werden soll</param>
        [TestCase("18.10.2016", "SetIntialTransactionsTest.csv", false)]
        public void UpdateInitialPortfolioTest(string asof, string filename, bool showInFile)
        {
            //datum parsen
            var date = DateTime.Parse(asof);

            //price history initialisieren
            if (_priceHistoryDictionary == null)
                _priceHistoryDictionary = TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx", date.AddDays(-250), date.AddDays(10));

            //Test Pm erstelle
            var pm = new PortfolioManager(null, null,
                new TransactionsHandler(null,
                new TransactionsCacheProviderTest(() => LoadHistory(filename))));

            //scoring provider erstellen
            var scoringProvider = new ScoringProvider(_priceHistoryDictionary);

            //scoring Provider registrieren
            pm.RegisterScoringProvider(scoringProvider);

            //einen CandidatesProvider erstellen
            var candidatesProvider = new CandidatesProvider(scoringProvider);

            //die Candidatenliste zurückgeben lassen
            var candidates = candidatesProvider.GetCandidates(date);

            //dem Portfolio Manager die potentiellen Kandidaten übergeben
            pm.PassInCandidates(candidates.ToList(), date);

            //Transaktionen speichern und im File anzeigen
            var saveProvider = new TestSaveProvider("TempPortfolio.csv", showInFile);
            pm.TemporaryPortfolio.SaveTransactions(saveProvider);

            //den pm erstelle ich nur um das Temp Portfolio File zu testen
            var pm2 = new PortfolioManager(null, null,
                new TestTransactionsHandler(null,
                new TransactionsCacheProviderTest(() => LoadHistory(saveProvider.TempPath))));

            //die Summe der Positionen
            var portfolioWeight = pm2.CurrentPortfolio.Sum(x => x.EffectiveWeight);

            //print zu debugg zwecken
            Trace.TraceInformation($"Portfoliogewicht: {portfolioWeight}");

            Assert.IsTrue(pm2.CurrentPortfolio.Any() && portfolioWeight <= new decimal(1));

        }

        [TestCase("01.01.2000", 5, "ConsTransactions.csv", true, true, "ConsPortfolioValue.csv", "ConsCash.csv")]
        public void SimpleBacktestTest(string startDate, int testYears, string temporaryFilename, bool showTransactions, bool clearOldFile, string navlogName, string cashLoggerName)
        {
            var filename = Path.Combine(Path.GetTempPath(), temporaryFilename);
            if (clearOldFile && File.Exists(filename))
                File.Delete(filename);
            if (!File.Exists(filename))
            {
                using (var file = File.Create(filename)) { }
            }

            //NLog konfigurieren
            //ConfigureLogger(navlogName, cashLoggerName);

            //datum parsen
            var date = DateTime.Parse(startDate);

            //price history initialisieren
            if (_priceHistoryDictionary == null)
                _priceHistoryDictionary = TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx", date.AddDays(-250), date.AddYears(testYears));

            //Test Pm erstelle
            var pm = new PortfolioManager(null, null,
                new TestTransactionsHandler(null,
                    new TransactionsCacheProviderTest(() => LoadHistory(filename))));

            //scoring provider erstellen
            var scoringProvider = new ScoringProvider(_priceHistoryDictionary);

            //scoring Provider registrieren
            pm.RegisterScoringProvider(scoringProvider);

            var navLogger = LogManager.GetLogger(Path.GetFileNameWithoutExtension(navlogName));
            navLogger.Info($"PortfolioAsof|PortfolioValue|AllocationToRisk");
            pm.PortfolioAsofChangedEvent += (sender, args) =>
            {
                navLogger.Info($"{args.ToShortDateString()} | {pm.PortfolioValue.ToString("N", CultureInfo.InvariantCulture)} | {pm.AllocationToRisk.ToString("N", CultureInfo.InvariantCulture)}");
            };

            var cashLogger = LogManager.GetLogger(Path.GetFileNameWithoutExtension(cashLoggerName));
            pm.CashHandler.CashChangedEvent += (sender, args) =>
            {
                cashLogger.Info($"{args.ToShortDateString()} | {pm.CashHandler.Cash.ToString("N", CultureInfo.InvariantCulture)}");
            };

            //einen BacktestHandler erstellen
            var candidatesProvider = new CandidatesProvider(scoringProvider);

            //backtestHandler erstellen
            var backtestHandler = new BacktestHandler(pm, candidatesProvider, new TestSaveProvider(temporaryFilename));

            //Backtest
            backtestHandler.RunBacktest(date, date.AddYears(testYears)).Wait();

            //schau nach ob in dem generierten File etwas drinnen steht
            using (var rd = new StreamReader(File.Open(filename, FileMode.Open)))
            {
                var output = rd.ReadToEnd();
                var transactions = SimpleTextParser.GetListOfType<Transaction>(output);
                Assert.IsTrue(!string.IsNullOrEmpty(output));
                Assert.IsTrue(transactions.Count > 10);
                Trace.TraceInformation($"Anzahl der Transaktionen :{transactions.Count}");
            }
            var factor = pm.PortfolioValue / pm.PortfolioSettings.InitialCashValue;
            var result = Math.Pow((double)factor, (double)1 / testYears);

            Trace.TraceInformation($"aktuelles Ergebnis kumuliert: {factor:P} {Environment.NewLine}aktuelles Ergebnis p.a.: {result - 1:P}");
        }

        [TestCase("RebalancePortfolioTestFile.txt", "19.01.2000")]
        public void RebalanceTest(string filename, string asof)
        {
            //Test Pm erstelle
            var pm = new PortfolioManager(null, null,
                new TestTransactionsHandler(null,
                    new TransactionsCacheProviderTest(() => LoadHistory(filename))));

            //datum parsen
            var date = DateTime.Parse(asof);

            //price history initialisieren
            if (_priceHistoryDictionary == null)
                _priceHistoryDictionary = TestHelper.CreateTestDictionary("EuroStoxx50Member.xlsx", date.AddDays(-270), date.AddYears(5));


            //scoring provider erstellen
            var scoringProvider = new ScoringProvider(_priceHistoryDictionary);
            pm.RegisterScoringProvider(scoringProvider);
            var candidatesProvider = new CandidatesProvider(scoringProvider);
            var candidates = candidatesProvider.GetCandidates(date).ToList();

            pm.PassInCandidates(candidates, date);

            Assert.IsTrue(pm.TemporaryPortfolio.HasChanges);
            Assert.IsFalse(pm.TemporaryPortfolio.GroupBy(x => x.SecurityId).Any(y => y.Key > 1));

        }


        [TestCase("CashTest.txt", "15.10.2017")]
        public void TestCash(string filename, string asof)
        {
            //Test Pm erstelle
            var pm = new PortfolioManager(null, null,
                new TestTransactionsHandler(null,
                    new TransactionsCacheProviderTest(() => LoadHistory(filename))));

            ////scoring provider erstellen
            //var scoringProvider = new ScoringProvider(_priceHistoryDictionary);

            ////scoring Provider registrieren
            //pm.RegisterScoringProvider(scoringProvider);

            pm.CashHandler.TryHasCash(out var remainingCash);
            //pm.CashHandler.GetCurrentCash();

            //einen CandidatesProvider erstellen
            //var candidatesProvider = new CandidatesProvider(scoringProvider);

            ////datum parsen
            //var date = DateTime.Parse(asof);

            ////die Candidatenliste zurückgeben lassen
            //var candidates = candidatesProvider.GetCandidates(date);

            ////dem Portfolio Manager die potentiellen Kandidaten übergeben
            //pm.PassInCandidates(candidates?.ToList() ?? new List<TradingCandidate>(), date);
        }


        [TestCase]
        public void CashMetaInfoTest()
        {
            var teststring =
                "Asof;Cash\r\n05.01.2000 00:00:00;90028.99\r\n05.01.2000 00:00:00;80034.7314\r\n05.01.2000 00:00:00;70038.2082\r\n05.01.2000 00:00:00;60054.9582\r\n05.01.2000 00:00:00;50058.3002\r\n05.01.2000 00:00:00;40058.6772\r\n05.01.2000 00:00:00;30115.6372\r\n05.01.2000 00:00:00;20131.0368\r\n05.01.2000 00:00:00;10148.1931\r\n05.01.2000 00:00:00;197.1931\r\n19.01.2000 00:00:00;-9599.5869\r\n19.01.2000 00:00:00;915.6135\r\n19.01.2000 00:00:00;-9709.7285\r\n19.01.2000 00:00:00;1988.9248\r\n19.01.2000 00:00:00;-9870.0752\r\n19.01.2000 00:00:00;1393.9248\r\n19.01.2000 00:00:00;-10158.3743\r\n19.01.2000 00:00:00;1072.7258\r\n19.01.2000 00:00:00;-10477.2742\r\n19.01.2000 00:00:00;117.9758\r\n02.02.2000 00:00:00;-15740.2242\r\n02.02.2000 00:00:00;-3933.3375\r\n02.02.2000 00:00:00;-19998.7370\r\n02.02.2000 00:00:00;-7784.8116\r\n02.02.2000 00:00:00;-19591.6983\r\n02.02.2000 00:00:00;1382.0259\r\n22.02.2000 00:00:00;4795.5111\r\n15.03.2000 00:00:00;24186.9106\r\n16.03.2000 00:00:00;57609.2106\r\n16.03.2000 00:00:00;67674.2436\r\n21.03.2000 00:00:00;76345.7436";

            var ret = SimpleTextParser.GetListOfType<CashMetaInfo>(teststring);

            Assert.IsTrue(ret.Count > 0);
        }
    }
}
