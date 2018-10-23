using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Common.Lib.Extensions;
using Common.Lib.UI.WPF.Core.Styling;
using HelperLibrary.Database.Models;
using HelperLibrary.Trading;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            //base Path from eecuting assembly
            var basePath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"../../../"));
            Globals.BasePath = basePath;
            ////Pfde in globals schreiben
            //var transactionsPaths = Directory.GetFiles(Path.Combine(basePath, @"Data/Transactions"), "*.*");
            //Globals.TransactionsPath = transactionsPaths[0];
            var priceHistoryPaths = Directory.GetFiles(Path.Combine(basePath, @"Data/PriceHistory"), "*.xls*");
            Globals.PriceHistoryPath = priceHistoryPaths[0];
            //var portfolioValuePaths = Directory.GetFiles(Path.Combine(basePath, @"Data/PortfolioValue"), "*.*");
            //Globals.PortfolioValuePath = portfolioValuePaths[0];
            //var portfolioCashPaths = Directory.GetFiles(Path.Combine(basePath, @"Data/Cash"), "*.*");
            //Globals.CashPath = portfolioCashPaths[0];


            //transaktinen Parse
            //  var transactions = Factory.CreateCollectionFromFile<Transaction>(transactionsPaths[0]).CastToList<ITransaction>();

            //scoring provider erstellen
            var scoringProvider = new ScoringProvider(Factory.CreatePriceHistoryFromFile(Globals.PriceHistoryPath, new DateTime(1999, 01, 01), new DateTime(2005, 01, 01)));

            //main window erstellen
            var mainwindow = new MainWindow { DataContext = new TradingViewModel(scoringProvider) };

            //und setzten und anzeigen
            Current.MainWindow = mainwindow;

            ThemeHandler.SetTheme(this, Themes.System);

            Current.MainWindow.Show();

            base.OnStartup(e);

        }
    }
}
