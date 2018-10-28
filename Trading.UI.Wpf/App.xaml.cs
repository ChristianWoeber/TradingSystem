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
            Globals.BasePath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"../../../"));
            Globals.PriceHistoryFilePath = Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/PriceHistory/EuroStoxx50Member.xlsx"));
            Globals.IndicesBasePath= Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/Indices"));

            //scoring provider erstellen
            var scoringProvider = new ScoringProvider(Factory.CreatePriceHistoryFromFile(Globals.PriceHistoryFilePath, new DateTime(1999, 01, 01), new DateTime(2005, 01, 01)));

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
