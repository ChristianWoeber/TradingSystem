using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Trading.Calculation.Collections;
using Trading.Core.Models;
using Trading.Core.Scoring;
using Trading.Core.Settings;
using Trading.Core.Transactions;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;
using Trading.PortfolioManager.UI.Wpf.ViewModels;

namespace Trading.PortfolioManager.UI.Wpf
{
    public static class Globals
    {
        /// <summary>
        /// Pfad der Assembly
        /// </summary>
        public static string BasePath { get; set; }

        /// <summary>
        /// Pfad der Directory on der die PriceHistories liegen
        /// </summary>
        public static string PriceHistoryDirectory { get; set; }
        public static bool IsTestMode { get; internal set; }
    }

    public class PortfolioManagerTransactionsCache : ITransactionsCacheProvider
    {
        public PortfolioManagerTransactionsCache(Func<Dictionary<int, List<ITransaction>>> loadFunc)
        {
            TransactionsCache = new Lazy<Dictionary<int, List<ITransaction>>>(loadFunc);
        }


        /// <summary>
        /// der Speicher mit den Transaktionen, nach SECID und der Liste von Transaktionen zu dem Wertpapier
        /// </summary>
        public Lazy<Dictionary<int, List<ITransaction>>> TransactionsCache { get; }

        /// <summary>
        /// Methode um den Speicher upzudaten
        /// </summary>
        public void UpdateCache()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private static volatile object _lockObject = new object();

        /// <summary>Löst das <see cref="E:System.Windows.Application.Startup" />-Ereignis aus.</summary>
        /// <param name="e">Ein <see cref="T:System.Windows.StartupEventArgs" />, das die Ereignisdaten enthält.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            Globals.BasePath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"../../../"));
            Globals.PriceHistoryDirectory = Path.GetFullPath(Path.Combine(Globals.BasePath, @"Data/PriceHistory"));
            //PM Initialisieren

            var scoringProvider = new ScoringProvider(CreatePriceHistoryFromSingleFiles());
            var pm = new Core.Portfolio.PortfolioManager(new DefaultStopLossSettings(),
                new ConservativePortfolioSettings(),
                new TransactionsHandler(new PortfolioManagerTransactionsCache(LoadTransactions)));

            pm.RegisterScoringProvider(scoringProvider);
            var model = new PortfolioManagerViewModel(pm);

            var win = new MainWindow { DataContext = model };
            MainWindow = win;
            MainWindow.Show();

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;


        }

        public Dictionary<int, List<ITransaction>> LoadTransactions()
        {
            return null;
        }

        public static Dictionary<int, IPriceHistoryCollection> CreatePriceHistoryFromSingleFiles()
        {
            //der Return Value
            var dic = new Dictionary<int, IPriceHistoryCollection>();

            if (!Directory.Exists(Globals.PriceHistoryDirectory))
                throw new ArgumentException(@"Am angegeben Pfad exisitert keine Datei !", Globals.PriceHistoryDirectory);

            var sw = Stopwatch.StartNew();

            //Parallel For Each ist in diesem Fall um den Faktor der kerne schneller
            Parallel.ForEach(Directory.GetFiles(Globals.PriceHistoryDirectory, "*.csv"), (file, state) =>
            {
                if (string.IsNullOrWhiteSpace(file))
                    return;

                var fileName = Path.GetFileNameWithoutExtension(file);
                //Id und Name parsen aus dem File
                //var split = Path.GetFileNameWithoutExtension(file).Split('_');

                //der Idx des letzten underscores
                var idx = Path.GetFileNameWithoutExtension(file).LastIndexOf('_');

                //Wenn keine Id gefunden wurde die Security ignorieren
                if (idx == -1)
                    return;

                var name = fileName.Substring(0, idx).Trim('_');
                var id = Convert.ToInt32(fileName.Substring(idx, fileName.Length - idx).Trim('_'));

                //die TradingRecords auslesen
                var data = SimpleTextParser.GetItemsOfTypeFromFilePath<TradingRecord>(file);
                //settings erstellen
                var settings = new PriceHistorySettings { Name = name };
                //im dictionary merken
                dic.Add(id, PriceHistoryCollection.Create(data, settings));

                //bei 100 einträge stoppen, dass reicht zum testen
                if (Globals.IsTestMode && dic.Count >= 100)
                    state.Stop();

            });
            sw.Stop();
            Trace.TraceInformation($"Dauer des Ladevorgangs in Sekunden: {sw.Elapsed.TotalSeconds:N}");
            return dic;
        }
    }
}
