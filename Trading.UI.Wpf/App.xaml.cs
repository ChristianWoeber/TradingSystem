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

            Globals.IsTestMode = true;

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
}
