using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Arts.ADB;
using Arts.Financial;
using Arts.Util;
using Common.Lib.UI.WPF.Core.Styling;
using CreateTestDataConsole;
using Trading.Calculation.Collections;
using Trading.Core.Models;
using Trading.Core.Scoring;
using Trading.DataStructures.Interfaces;
using Trading.UI.Wpf.Utils;
using Trading.UI.Wpf.ViewModels;

namespace Trading.UI.Wpf
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private static readonly IFXRateProvider _fxRateProvider = new ArtsFXRateProvider(600);
        protected override void OnStartup(StartupEventArgs e)
        {
            //base Path from executing assembly
            Globals.BasePath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"../../../"));
            Globals.PriceHistoryFilePath = Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/PriceHistory/EuroStoxx50Member.xlsx"));
            Globals.IndicesBasePath = Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/Indices"));
            Globals.PriceHistoryDirectory = Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/PriceHistory"));
            Globals.TransactionsDirectory = Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/Transactions"));
            Globals.PortfolioValuationDirectory = Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/PortfolioValue"));

            Globals.IsTestMode = false;

            //var dlg = SmartDialog.Create(new SmartDialogDefaultConfiguration("Eingabe erforderlich",
            //    "Möchten Sie den Offline-Modus verwenden?", null){Buttons = SmartDialogButtons.YesAndNo});

            //erstelle den Scoring provider entwerder direkt aus den Fints oder im Offline Modus aus dem Files
            //var scoringProvider = dlg.ShowDialog() == false
            //    ? CreateScoringProviderFromArtsDb()
            //    : new ScoringProvider(BootStrapperFactory.CreatePriceHistoryFromSingleFiles(Globals.PriceHistoryDirectory));


            var scoringProvider = new NewHighsCountScoringProvider(BootStrapperFactory.CreatePriceHistoryFromSingleFiles(Globals.PriceHistoryDirectory));

            //main window erstellen
            var mainwindow = new MainWindow { DataContext = new TradingViewModel(scoringProvider) };

            //und setzten und anzeigen
            Current.MainWindow = mainwindow;

            ThemeHandler.SetTheme(this, Themes.System);

            Current.MainWindow.Show();

            base.OnStartup(e);

        }

        private static ScoringProvider CreateScoringProviderFromArtsDb()
        {
            var priceHistroyCollection = new Dictionary<int, IPriceHistoryCollection>();
            var idNameDictionary = IndexMember.StoxxEurope600IdNameTuples.ToDictionary(key => key.Item1, value => value.Item2);
            var multiFints = DBTools.ADBQueryMultiFINTS<double>("STOCKQUOTES",
                IDFilter.List(IndexMember.StoxxEurope600IdNameTuples.Select(x => x.Item1 - 400000)),
                IDFilter.Single(1));
            var ccyDictionary = SQLCmd.Select("trading", "stocks").Fields("id_,ccy").Equal("active", 1)
                .QueryMap<int, string>();
            FXR conversion = null;

            foreach (var fintsRecord in multiFints)
            {
                if (ccyDictionary.TryGetValue(fintsRecord.ID1, out var currency))
                {
                    if (currency != "EUR")
                    {
                        if (currency == "GBp")
                            continue;

                        var currencyFrom = _fxRateProvider.GetCurrency(currency);
                        conversion = _fxRateProvider.GetFXRates(currencyFrom.ID, 1);
                    }
                }

                var fints = fintsRecord.CreateFINTS();
                fints = fints.ConvertFX(conversion);
                fints.Caption = idNameDictionary.TryGetValue(fintsRecord.ID1 + 400000, out var name) ? name : null;
                //settings erstellen
                var setting = new PriceHistorySettings { Name = name };
                //records erstellen
                var tradingRecords = fints.Select(x => new TradingRecord
                {
                    AdjustedPrice = x.DecimalPrice.Value,
                    Asof = x.Date.ToDateTime(),
                    Name = name,
                    Price = x.DecimalPrice.Value,
                    SecurityId = fintsRecord.ID1
                });
                priceHistroyCollection.Add(fintsRecord.ID1, PriceHistoryCollection.Create(tradingRecords, setting));
            }

            return new ScoringProvider(priceHistroyCollection);
        }
    }

    public class NewHighsCountScoringProvider : IScoringProvider
    {
        public NewHighsCountScoringProvider(Dictionary<int, IPriceHistoryCollection> createPriceHistoryFromSingleFiles)
        {
            PriceHistoryStorage = createPriceHistoryFromSingleFiles;
        }

        /// <summary>
        /// Storage of alle price histories, key => id, value the IPriceHistoryCollection
        /// </summary>
        public IDictionary<int, IPriceHistoryCollection> PriceHistoryStorage { get; }

        /// <summary>
        /// The Method that Returns the Scoring Result
        /// </summary>
        /// <param name="secId">the id of the security</param>
        /// <param name="date">the start date for the Calculations</param>
        public IScoringResult GetScore(int secId, DateTime date)
        {
            if (!PriceHistoryStorage.TryGetValue(secId, out var priceHistory))
                return new NewHighsCountScoringResult();

            //Das Datum des NAVs der in der PriceHistoryCollection gefunden wurde
            var priceHistoryRecordAsOf = priceHistory.Get(date)?.Asof;
            if (priceHistoryRecordAsOf == null)
                return new NewHighsCountScoringResult();

            //die 250 Tages Performance
            var performance250 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-250), date);

            //Wenn keine Berechungn der 250 Tages Performance möglich ist, returne ich false
            if (performance250 == -1)
                return new NewHighsCountScoringResult();

            //Alle Berechnungnen durchführen
            var performance10 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-10), date);
            var performance30 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-30), date);
            var performance90 = priceHistory.Calc.GetAbsoluteReturn(date.AddDays(-90), date);
            if (!priceHistory.Calc.TryGetLastVolatilityInfo(date, out var volaInfo))
                return new NewHighsCountScoringResult();
            if (!priceHistory.TryGetLowMetaInfo(date, out var lowMetaInfo))
                return new NewHighsCountScoringResult();

            // Das Ergebnis returnen
            return new NewHighsCountScoringResult
            {
                Asof = priceHistoryRecordAsOf.Value,
                Performance10 = performance10,
                Performance30 = performance30,
                Performance90 = performance90,
                Performance250 = performance250,
                //MaxDrawdown = maxDrawDown,
                Volatility = volaInfo.DailyVolatility,
                LowMetaInfo = lowMetaInfo
            };
        }

        /// <summary>
        /// Returns then ITradingRecord at the given Date
        /// </summary>
        /// <param name="securityId">the security id</param>
        /// <param name="asof">The Datetime of the record</param>
        /// <returns></returns>
        public ITradingRecord GetTradingRecord(int securityId, DateTime asof)
        {
            return !PriceHistoryStorage.TryGetValue(securityId, out var priceHistoryCollection)
                ? null
                : priceHistoryCollection.Get(asof);
        }
    }

    public class NewHighsCountScoringResult : ScoringResult
    {
        public NewHighsCountScoringResult() : base()
        {

        }

        /// <summary>der Score der sich aus den Berechnungen ergibt</summary>
        public override decimal Score
        {
            get
            {
                if (LowMetaInfo == null)
                    return 0;

                // Ich gewichte die Performance,
                // die aktuellsten Daten haben die größten Gewichte
                //ich ziehe auch noch den maxdrawdown in der Periode ab
                var avgPerf = Performance10 * (decimal)0.20
                              + Performance30 * (decimal)0.30
                              + Performance90 * (decimal)0.40
                              + Performance250 * (decimal)0.10;

                var increaseFactor = 1 + LowMetaInfo.NewHighsCount * 0.1M;
                var newHighsAdjusted = avgPerf * increaseFactor;

                //danach zinse ich quais die vola ab wenn null dann nehm ich als default 20%
                return Math.Round(newHighsAdjusted * (1 - Volatility ?? 0.35M) * 100, 2);
            }
        }
    }
}
