using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Common.Lib.Extensions;
using Common.Lib.UI.WPF.Core.Controls.Core;
using Common.Lib.UI.WPF.Core.Controls.Dialog;
using Common.Lib.UI.WPF.Core.Input;
using Common.Lib.UI.WPF.Core.Primitives;
using CreateTestDataConsole;
using JetBrains.Annotations;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Trading.Calculation.Collections;
using Trading.Core.Backtest;
using Trading.Core.Candidates;
using Trading.Core.Cash;
using Trading.Core.Exposure;
using Trading.Core.Models;
using Trading.Core.Portfolio;
using Trading.Core.Settings;
using Trading.Core.Transactions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;
using Trading.UI.Wpf.Models;
using Trading.UI.Wpf.ViewModels.EventArgs;
using Trading.UI.Wpf.Windows;

namespace Trading.UI.Wpf.ViewModels
{
    public class TradingViewModel : INotifyPropertyChanged, ISmartBusyRegion
    {
        #region Private Members

        private PortfolioManager _portfolioManager;
        private readonly IScoringProvider _scoringProvider;
        private ObservableCollection<TransactionViewModel> _holdings;
        private DateTime _startDateTime;
        private DateTime _endDateTime;
        private CancellationTokenSource _cancellationSource;
        private bool _isBusy;
        private CashInfoCollection _cashInfosDictionary;

        #endregion

        #region Constructor

        public TradingViewModel()
        {
            Settings = new SettingsViewModel(new ConservativePortfolioSettings());
            IndexSettings = new IndexBacktestSettings();
            StartDateTime = new DateTime(2000, 01, 01);
            EndDateTime = StartDateTime.AddYears(2);


            //Command
            RunNewBacktestCommand = new RelayCommand(OnRunBacktest);
            RunNewIndexBacktestCommand = new RelayCommand(OnRunIndexBacktest);
            LoadBacktestCommand = new RelayCommand(OnLoadBacktest);
            MoveCursorToNextTradingDayCommand = new RelayCommand(() => MoveCursorToNextTradingDayEvent?.Invoke(this, _portfolioManager.PortfolioSettings.TradingDay));
            MoveCursorToNextStoppDayCommand = new RelayCommand(() => MoveCursorToNextStoppDayEvent?.Invoke(this, System.EventArgs.Empty));
            ShowSelectedPositionCommand = new RelayCommand(ShowNewSelectedPositionWindow);
            ShowSelectedCandidateCommand = new RelayCommand(ShowSelectedCandidateWindow);
            ShowSelectedTradeCommand = new RelayCommand(OnShowSelectedTradeWindow);
            SaveBacktestCommand = new RelayCommand(OnSaveBacktest);
            TradeStatisticsCommand = new RelayCommand(OnCalculateTradeStatistics);
            ShowTradesCommand = new RelayCommand((o) => OnShowTrades(), (o) => TransactionsRepo.IsInitialized);
            BlockSelectedCandidateFromBacktestCommand = new RelayCommand((o) => _backtestHandler.AddBlockedCandidate(SelectedCandidate.SecurityId), (o) => SelectedCandidate != null);
            ShowTradingCandidatesCommand = new RelayCommand((o) => OnShowTradingCandidates(), (o) => Candidates != null);

        }


        public TradingViewModel(IScoringProvider scoringProvider) : this()
        {
            _scoringProvider = scoringProvider;
        }


        public ObservableCollection<TradeViewModel> Trades { get; } = new ObservableCollection<TradeViewModel>();



        private void OnShowTradingCandidates()
        {
            var win = new TradingCandidatesWindow() { DataContext = this };
            win.Show();
        }

        private void OnShowTrades()
        {
            if (Trades.Count == 0)
            {
                foreach (var transactionGroup in TransactionsRepo.GetAllTransactions().GroupBy(x => x.SecurityId))
                {
                    var count = 0;
                    var transactions = transactionGroup.ToList();
                    while (count != transactions.Count)
                    {
                        var trades = CreateTradesFromTransactions(transactions, ref count);
                        Trades.Add(new TradeViewModel(new Trade(trades, EndDateTime)));
                    }
                }
            }

            var win = new TradesWindow { DataContext = this };
            win.Show();
        }

        private List<Transaction> CreateTradesFromTransactions(List<Transaction> transactionSource, ref int count)
        {
            var trades = new List<Transaction>();
            for (var i = count; i < transactionSource.Count; i++)
            {
                var transaction = transactionSource[i];
                count++;
                if (transaction.TransactionType != TransactionType.Close)
                {
                    trades.Add(transaction);
                    continue;
                }

                //bin hier beim close
                trades.Add(transaction);
                break;
            }

            return trades;
        }

        private async void OnCalculateTradeStatistics()
        {
            var fileDlg = new CommonOpenFileDialog()
            {
                DefaultDirectory = Settings.LoggingPath,
                Filters = { new CommonFileDialogFilter("*.", "*.zip") }
            };
            var dlgResult = fileDlg.ShowDialog();
            if (dlgResult != CommonFileDialogResult.Ok)
                return;

            //unzippen
            var filePath = UnZipToTempDirectory(fileDlg.FileName);

            //das File aus dem Temp verzeichnis parsen und priceHistoryColleciion erstellen
            var valuations =
                 SimpleTextParser.GetListOfTypeFromFilePath<PortfolioValuation>(Path.Combine(filePath,
                     "PortfolioValuations.csv"));
            var priceHistory = PriceHistoryCollection.Create(valuations.Select(v =>
               new TradingRecord
               {
                   AdjustedPrice = v.PortfolioValue,
                   Asof = v.PortfolioAsof,
                   Name = "NAV - Backtest",
                   Price = v.PortfolioValue,
                   SecurityId = -100
               }));

            if (priceHistory == null)
                throw new ArgumentException($"Achtung das Datum darf nicht null sein, anscheinend keine Werte" +
                                            $" in der PrichistoryCollection mit dem Pfad {filePath}");

            await priceHistory.Calc.CreateRollingPeriodeResultsTask(3, 5, 10, 15);

            foreach (var histogramm in priceHistory.Calc.EnumHistogrammClasses())
            {
                foreach (var result in histogramm)
                {
                    Trace.TraceInformation($"Für das {result.PeriodeInYears} Jahres-Fenster lagen {result.RelativeFrequency:p2} " +
                                           $"der Daten im Bereich von {result.Minimum.PerformanceCompound:p2} und {result.Maximum.PerformanceCompound:p2}");
                }
            }
        }

        private async void OnShowSelectedTradeWindow(object obj)
        {
            if (!(obj is TradeViewModel model))
                return;

            await ShowChartWindowAsync(model.SecurityId ?? -1, model.OpenDateTime.Value);
        }

        private async void ShowSelectedCandidateWindow()
        {
            if (SelectedCandidate == null)
                return;

            if (!_scoringProvider.PriceHistoryStorage.TryGetValue(SelectedCandidate.SecurityId, out var priceHistoryCollection))
                return;

            var win = new ChartWindow(SelectedCandidate.SecurityId, ChartDate);

            await win.CreateFints(priceHistoryCollection, NameCatalog[SelectedCandidate.SecurityId], false);

            win.Show();
        }

        private async void ShowNewSelectedPositionWindow()
        {
            if (SelectedPosition == null)
                return;

            await ShowChartWindowAsync(SelectedPosition.SecurityId, SelectedPosition.TransactionDateTime);
        }

        private async Task ShowChartWindowAsync(int securityId, DateTime? date = null)
        {
            if (!_scoringProvider.PriceHistoryStorage.TryGetValue(securityId, out var priceHistoryCollection))
            {
                //wenn es noch keine History zu dem Kandidaten gibt lade ich diesen dynamisch nach
                if (Globals.IsTestMode)
                {
                    await Task.Run(() =>
                    {
                        //namen des Files erstellen
                        var name = $"{NameCatalog[securityId]}_{securityId}.csv";
                        var filename = Path.Combine(Globals.PriceHistoryDirectory, name);
                        //die records parsen
                        var history = SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(filename);
                        Application.Current.Dispatcher?.Invoke(() =>
                        {
                            //die Pricehistory erstellen und storen im dictionary
                            priceHistoryCollection =
                                PriceHistoryCollection.Create(history, new PriceHistorySettings { Name = name });
                            _scoringProvider.PriceHistoryStorage.Add(SelectedPosition?.SecurityId ?? securityId, priceHistoryCollection);
                        });
                    });
                }
                else
                    return;
            }

            var win = new ChartWindow(securityId, date ?? ChartDate);
            await win.CreateFints(priceHistoryCollection, NameCatalog[securityId]);
            win.Show();
        }


        #endregion

        #region Events

        public event EventHandler<IndexBacktestResultEventArgs> IndexBacktestCompletedEvent;

        public event EventHandler<BacktestResultEventArgs> BacktestCompletedEvent;

        public event EventHandler<DayOfWeek> MoveCursorToNextTradingDayEvent;

        public event EventHandler MoveCursorToNextStoppDayEvent;

        #endregion

        #region Commands

        public ICommand TradeStatisticsCommand { get; }

        public ICommand RunNewIndexBacktestCommand { get; }

        public ICommand RunNewBacktestCommand { get; }

        public ICommand LoadBacktestCommand { get; }

        public ICommand MoveCursorToNextTradingDayCommand { get; }

        public ICommand MoveCursorToNextStoppDayCommand { get; }

        public ICommand ShowTradingCandidatesCommand { get; }

        public ICommand ShowSelectedPositionCommand { get; }

        public ICommand ShowSelectedCandidateCommand { get; }

        public ICommand ShowSelectedTradeCommand { get; }

        public ICommand SaveBacktestCommand { get; }

        public ICommand ShowTradesCommand { get; }

        public ICommand BlockSelectedCandidateFromBacktestCommand { get; }

        #endregion

        #region Initializations

        private void Init(List<ITransaction> transactions)
        {
            _portfolioManager = new PortfolioManager(null
                , Settings
                , new TransactionsHandler(null, new BacktestTransactionsCacheProvider(transactions)));

            //scoring Provider registrieren
            _portfolioManager.RegisterScoringProvider(_scoringProvider);

            //BacktestCompleted Event feuern
            var portfolioValuation = SimpleTextParser.GetListOfType<PortfolioValuation>(Path.Combine(_portfolioManager.PortfolioSettings.LoggingPath, "PortfolioValue"));
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(portfolioValuation, null, Settings));
        }


        #endregion

        #region CommandActions

        private async void OnRunIndexBacktest()
        {
            using (SmartBusyRegion.Start(this))
            {
                var indexOutput = new IndexResult { IndicesDirectory = Globals.IndicesBasePath };
                var exposureWatcher = new ExposureWatcher(indexOutput, new FileExposureDataProvider(Globals.PriceHistoryDirectory));
                var backtestHandler = new BacktestHandler(exposureWatcher);
                _cancellationSource = new CancellationTokenSource();
                await backtestHandler.RunIndexBacktest(StartDateTime, EndDateTime, _cancellationSource.Token);

                IndexBacktestCompletedEvent?.Invoke(this, new IndexBacktestResultEventArgs(backtestHandler.IndexResults.CastToList<IIndexBackTestResult>()));
            }
        }

        private async void OnRunBacktest()
        {
            //Pm erstellen für den Backtest
            using (var region = SmartBusyRegion.Start(this, true))
            {
                ProgressRegion = region.ProgessRegion;
                //Clean up
                var files = Directory.GetFiles(Settings.LoggingPath);
                foreach (var file in files.Where(x => x.EndsWith(".csv") || x.EndsWith(".json")))
                    File.Delete(file);

                //Save Settings in JSON
                var ser = JsonConvert.SerializeObject(Settings.PortfolioSettings);
                var fullJsonPath = Path.Combine(Settings.LoggingPath, "Settings.json");
                File.WriteAllText(fullJsonPath, ser);

                var transactionsPath = Path.Combine(Settings.LoggingPath, "Transactions.csv");
                InitializePortfolioManager(transactionsPath);

                var loggingProvider = new LoggingSaveProvider(Settings.LoggingPath, _portfolioManager);

                //einen BacktestHandler erstellen
                _candidatesProvider = new CandidatesProvider(_scoringProvider);

                //backtestHandler erstellen
                _backtestHandler = new BacktestHandler(_portfolioManager, _candidatesProvider, loggingProvider);

                //Backtest
                _cancellationSource = new CancellationTokenSource();
                await _backtestHandler.RunBacktest(StartDateTime, EndDateTime, _cancellationSource.Token, region.ProgessRegion);
            }

            //create output
            var valuations = SimpleTextParser.GetListOfTypeFromFilePath<PortfolioValuation>(Path.Combine(Settings.LoggingPath, "PortfolioValuations.csv"));
            var cashMovements = SimpleTextParser.GetListOfTypeFromFilePath<CashMetaInfo>(Path.Combine(Settings.LoggingPath, nameof(CashMetaInfo) + "s.csv"));
            var scoringTraces = SimpleTextParser.GetListOfTypeFromFilePath<ScoringTraceModel>(Path.Combine(Settings.LoggingPath, nameof(ScoringTraceModel) + ".csv"));

            //Repos initialisieren
            TransactionsRepo.Initialize(SimpleTextParser.GetListOfTypeFromFilePath<Transaction>(Path.Combine(Settings.LoggingPath, "Transactions.csv")));
            StoppLossRepository.Initialize(SimpleTextParser.GetListOfTypeFromFilePath<Transaction>(Path.Combine(Settings.LoggingPath, "StoppLoss" + nameof(Transaction) + "s.csv")));
            ScoringRepository.Initialize(scoringTraces);
            InitializeCashMovements(cashMovements);

            TransactionsCountTotal = TransactionsRepo.GetAllTransactionsCount();
            AveragePortfolioSize = await CalculateAveragePortfolioHoldings();
            PortfolioTurnOver = TransactionsCountPerWeek / AveragePortfolioSize;

            var resultWindow = new TradeStatisticsWindow { DataContext = this };
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(valuations, TransactionsRepo.GetAllTransactions(), Settings));
            resultWindow.Show();
        }

        private void InitializeCashMovements(List<CashMetaInfo> cashMovements)
        {
            if (_cashInfosDictionary?.Count > 0)
                _cashInfosDictionary.Clear();
            //CashInfos updaten
            _cashInfosDictionary = new CashInfoCollection(cashMovements);
        }

        private void InitializePortfolioManager(string transactionsPath)
        {
            var pm = new PortfolioManager(null, Settings,
                new TransactionsHandler(null, new BacktestTransactionsCacheProvider(() => LoadHistory(transactionsPath))));

            //scoring Provider registrieren
            pm.RegisterScoringProvider(_scoringProvider);
            pm.CalcStartingAllocationToRisk(StartDateTime);
            _portfolioManager = pm;
        }

        private Task<decimal> CalculateAveragePortfolioHoldings()
        {
            return Task.Run(() =>
            {
                var count = 1;
                var sum = 1M;
                foreach (var dateGrp in TransactionsRepo.GetAllTransactions().GroupBy(x => x.TransactionDateTime))
                {
                    var holdings = _portfolioManager.TransactionsHandler.GetCurrentHoldings(dateGrp.Key);
                    count++;
                    sum += holdings.Count();
                }

                return sum / count;
            });
        }

        private void OnCancel()
        {
            _cancellationSource?.Cancel();
        }


        private Dictionary<int, List<ITransaction>> LoadHistory(string filename)
        {
            return SimpleTextParser.GetListOfTypeFromFilePath<Transaction>(filename)?.OfType<ITransaction>()?.ToDictionaryList(x => x.SecurityId);
        }

        private async void OnSaveBacktest()
        {
            var dialog = new CommonOpenFileDialog { InitialDirectory = Settings.LoggingPath, IsFolderPicker = true };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            var nameDialog = SmartDialog.CreateCommentDialog(Settings);
            if (nameDialog.ShowDialog() == false)
                return;

            await CreateBacktestZipAsync(dialog.FileName);
        }

        public async Task CreateBacktestZipAsync(string targetDirectory)
        {
            await Task.Run(() => CreateBacktestZip(targetDirectory));
        }

        public void CreateBacktestZip(string targetDirectory)
        {
            var fileName = Path.Combine(targetDirectory, $"{Settings.BacktestDescription ?? "Backtest"}_{DateTime.Now:d}.zip");
            if (File.Exists(fileName))
                File.Delete(fileName);

            using (var zipMs = new FileStream(fileName, FileMode.CreateNew))
            {
                using (var zipArchiv = new ZipArchive(zipMs, ZipArchiveMode.Create, true))
                {
                    foreach (var filePath in Directory.GetFiles(Settings.LoggingPath))
                    {
                        if (Path.GetExtension(filePath).EndsWith(".zip"))
                            continue;

                        var entry = zipArchiv.CreateEntry(Path.GetFileName(filePath) ?? throw new InvalidOperationException("Achtung die Datei konnte nicht gefunden werden"));
                        using (var stream = entry.Open())
                        {
                            var bytes = File.ReadAllBytes(filePath);
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
        }


        private void OnLoadBacktest()
        {
            //file auswählen
            var dialog = new CommonOpenFileDialog { InitialDirectory = Settings.LoggingPath, Filters = { new CommonFileDialogFilter("*.", "*.zip") } };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            //unzippen
            var zipTempfilePath = UnZipToTempDirectory(dialog.FileName);

            var jsonSettings = Path.Combine(zipTempfilePath, "Settings.json");
            if (File.Exists(jsonSettings))
            {
                var ser = File.ReadAllText(jsonSettings);
                Settings.UpdateFromDeserializedSettings(JsonConvert.DeserializeObject<ConservativePortfolioSettings>(ser));
            }

            //Valuations, Stops & Transaktionen
            var valuations = SimpleTextParser.GetListOfType<PortfolioValuation>(File.ReadAllText(Path.Combine(zipTempfilePath, "PortfolioValuations.csv")));
            var transactions = SimpleTextParser.GetListOfType<Transaction>(File.ReadAllText(Path.Combine(zipTempfilePath, "Transactions.csv")));
            var stops = SimpleTextParser.GetListOfType<Transaction>(File.ReadAllText(Path.Combine(zipTempfilePath, "StoppLoss" + nameof(Transaction) + "s.csv")));
            var cashMovements = SimpleTextParser.GetListOfTypeFromFilePath<CashMetaInfo>(Path.Combine(zipTempfilePath, nameof(CashMetaInfo) + "s.csv"));
            var scoringTraces = SimpleTextParser.GetListOfTypeFromFilePath<ScoringTraceModel>(Path.Combine(zipTempfilePath, nameof(ScoringTraceModel) + ".csv"));

            //Repos initialisieren
            ScoringRepository.Initialize(scoringTraces);
            TransactionsRepo.Initialize(transactions);
            StoppLossRepository.Initialize(stops);
            InitializeCashMovements(cashMovements);
            if (_candidatesProvider == null)
                _candidatesProvider = new CandidatesProvider(_scoringProvider);

            //Wenn der Pm noch nicht initialisert wurde an dieser Stelle initialiseren
            if (_portfolioManager == null)
            {
                var path = Path.Combine(zipTempfilePath, $"{nameof(Transaction)}s.csv");
                if (!File.Exists(path))
                    MessageBox.Show($"Achtung das angebene File {path} existiert nicht!");
                InitializePortfolioManager(path);
            }

            //Event invoken
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(valuations, TransactionsRepo.GetAllTransactions(), Settings));
        }

        private string UnZipToTempDirectory(string filePath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetTempFileName().Replace(".tmp", ".zip"));
            ZipFile.ExtractToDirectory(filePath, tempDir);
            return tempDir;
        }

        #endregion

        #region Helpers

        public static Dictionary<int, string> NameCatalog => _nameCatalog ?? (_nameCatalog = CreateCatalog());

        private static Dictionary<int, string> CreateCatalog()
        {
            var dic = new Dictionary<int, string>();
            var unionedTuples = IndexMember.EuroStoxx50IdNameTuples.Union(IndexMember.SandP500IdNameTuples).Union(IndexMember.Nasdaq100IdNameTuples).Union(IndexMember.StoxxEurope600IdNameTuples);
            foreach (var (securityId, name) in unionedTuples)
            {
                if (dic.ContainsKey(securityId))
                    continue;
                dic.Add(securityId, name);
            }

            return dic;
        }

        private IEnumerable<CashMetaInfo> _cash;
        private IEnumerable<TransactionViewModel> _stopps;
        private IEnumerable<TradingCandidateViewModel> _candidates;
        private CandidatesProvider _candidatesProvider;
        private BacktestHandler _backtestHandler;
        private TransactionViewModel _selectedPosition;
        private TradingCandidateViewModel _selectedCandidate;
        private int _transactionsCountTotal;
        private decimal _portfolioTurnOver;
        private decimal _averagePortfolioSize;
        private static Dictionary<int, string> _nameCatalog;
        private SmartProgressRegion _progressRegion;

        public void UpdateCash(DateTime toDateTime)
        {
            if (_portfolioManager == null)
                return;

            if (_cashInfosDictionary.TryGetLastCash(toDateTime, out var infos))
                Cash = infos;
        }


        public void UpdateStops(DateTime asof)
        {
            //die Stopps erstellen
            Stopps = StoppLossRepository.GetStops(asof).Select(x => new TransactionViewModel(x, GetScore(x, x.TransactionDateTime), true));
        }

        public void UpdateHoldings(DateTime asof, bool isTradingDay = false)
        {
            if (_portfolioManager == null)
                return;

            UpdateCash(asof);
            UpdateStops(asof);
            UpdateCandidates(asof);
            var tradingDayTransaction = _portfolioManager.TransactionsHandler.GetTransactions(asof);
            if (tradingDayTransaction == null)
            {
                //Todo: auf Basis des Current Portfolio die ScoringResults holen
                var holdings = _portfolioManager.TransactionsHandler.GetCurrentHoldings(asof);
                Holdings = new ObservableCollection<TransactionViewModel>(holdings.Select(transaction => CreateTransactionsViewModel(transaction, asof)));
            }
            else
            {
                //ich returne am tading tag den portfoliostand vor der umschichtung + die umschichtungen separat, damit
                //die anzeige in der Gui klarer ist und nachvollzogen werden kann, was zu dem Stichtag geschehen ist
                Holdings = new ObservableCollection<TransactionViewModel>(_portfolioManager.TransactionsHandler.GetCurrentHoldings(asof.AddDays(-1)).Select(t => CreateTransactionsViewModel(t, asof))
                    .Concat(tradingDayTransaction.Select(t => CreateTransactionsViewModel(t, asof, true))));
            }

            var stopDic = Stopps.ToDictionaryList(x => x.SecurityId);

            foreach (var position in Holdings)
            {
                if (!stopDic.TryGetValue(position.SecurityId, out var stopps))
                    continue;

                if (stopps.Any(x => x.TransactionDateTime == position.TransactionDateTime))
                    position.IsStop = true;
            }



        }

        private TransactionViewModel CreateTransactionsViewModel(ITransaction t, DateTime asof, bool isNew = false)
        {
            var scoringTraceModel = GetScoreFromRepo(CreateKey(t, asof));
            return scoringTraceModel == null
                ? new TransactionViewModel(t, GetScore(t, asof)) { IsNew = isNew }
                : new TransactionViewModel(t, scoringTraceModel) { IsNew = isNew };
        }


        private string CreateKey(ITransaction transaction, DateTime asof)
        {
            return $"{asof:d}_{transaction.SecurityId}";

            //  return $"{transaction.TransactionDateTime:d}_{asof:d}_{transaction.Shares}_{(int)transaction.TransactionType}_{transaction.SecurityId}";
        }

        private void UpdateCandidates(DateTime asof)
        {
            Candidates = _candidatesProvider.GetCandidates(asof).Select((x, i) => new TradingCandidateViewModel(x, i));
        }

        private IScoringResult GetScore(ITransaction transaction, DateTime asof)
        {
            return _scoringProvider.GetScore(transaction.SecurityId, asof);
        }

        private ScoringTraceModel GetScoreFromRepo(string key)
        {
            return ScoringRepository.GetScore(key);
        }


        #endregion

        #region Public Members

        public TradingCandidateViewModel SelectedCandidate
        {
            get => _selectedCandidate;
            set
            {
                if (Equals(value, _selectedCandidate))
                    return;
                _selectedCandidate = value;
                OnPropertyChanged();
            }
        }


        public TransactionViewModel SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (Equals(value, _selectedPosition))
                    return;
                _selectedPosition = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Die TradingCandidaten zum jeweiligen Handelstag
        /// </summary>

        public IEnumerable<TradingCandidateViewModel> Candidates
        {
            get => _candidates;
            set
            {
                if (Equals(value, _candidates))
                    return;
                _candidates = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Das Cash, bzw die Casheinträge zum jeweiligen Handelstag
        /// </summary>
        public IEnumerable<CashMetaInfo> Cash
        {
            get => _cash;
            set
            {
                if (Equals(value, _cash))
                    return;
                _cash = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Die Holdings zum jeweiligen Handelstag
        /// </summary>
        public ObservableCollection<TransactionViewModel> Holdings
        {
            get => _holdings;
            set
            {
                if (Equals(value, _holdings))
                    return;
                _holdings = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Die aufgelaufenen Stopps zum jeweiligen Handelstag
        /// </summary>
        public IEnumerable<TransactionViewModel> Stopps
        {
            get => _stopps;
            set
            {
                if (Equals(value, _stopps))
                    return;
                _stopps = value;
                OnPropertyChanged();
            }
        }

        public DateTime StartDateTime
        {
            get => _startDateTime;
            set
            {
                if (value.Equals(_startDateTime))
                    return;
                _startDateTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndDateTime
        {
            get => _endDateTime;
            set
            {
                if (value.Equals(_endDateTime))
                    return;
                _endDateTime = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (value == _isBusy)
                    return;
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public IndexBacktestSettings IndexSettings { get; }

        public SettingsViewModel Settings { get; }

        public bool HasPortfolioManager => _portfolioManager != null;
        public DateTime ChartDate { get; set; }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion


        public SmartProgressRegion ProgressRegion
        {
            get => _progressRegion;
            set
            {
                if (Equals(value, _progressRegion))
                    return;
                _progressRegion = value;
                OnPropertyChanged();
            }
        }


        public int TransactionsCountTotal
        {
            get => _transactionsCountTotal;
            set
            {
                if (value == _transactionsCountTotal)
                    return;
                _transactionsCountTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TransactionsCountPerYear));
                OnPropertyChanged(nameof(TransactionsCountPerMonth));
                OnPropertyChanged(nameof(TransactionsCountPerWeek));

            }
        }

        public decimal TransactionsCountPerYear => TransactionsCountTotal / ((EndDateTime - StartDateTime).Days / (decimal)250);
        public decimal TransactionsCountPerMonth => TransactionsCountPerYear / 12;
        public decimal TransactionsCountPerWeek => TransactionsCountPerYear / 50;

        public decimal AveragePortfolioSize
        {
            get => _averagePortfolioSize;
            set
            {
                if (value == _averagePortfolioSize)
                    return;
                _averagePortfolioSize = value;
                OnPropertyChanged();
            }
        }

        public decimal PortfolioTurnOver
        {
            get => _portfolioTurnOver;
            set
            {
                if (value == _portfolioTurnOver)
                    return;
                _portfolioTurnOver = value;
                OnPropertyChanged();
            }
        }


    }
}