﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Arts.Financial;
using Common.Lib.Extensions;
using Common.Lib.UI.WPF.Core.Controls.Core;
using Common.Lib.UI.WPF.Core.Input;
using Common.Lib.UI.WPF.Core.Primitives;
using CreateTestDataConsole;
using HelperLibrary.Database.Models;
using HelperLibrary.Extensions;
using HelperLibrary.Parsing;
using HelperLibrary.Trading;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Trading.PortfolioManager.Cash;
using HelperLibrary.Trading.PortfolioManager.Exposure;
using HelperLibrary.Trading.PortfolioManager.Settings;
using HelperLibrary.Trading.PortfolioManager.Transactions;
using JetBrains.Annotations;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.UI.Wpf.Models;
using Trading.UI.Wpf.Utils;
using Trading.UI.Wpf.ViewModels.EventArgs;
using Trading.UI.Wpf.Windows;

namespace Trading.UI.Wpf.ViewModels
{
    public static class TransactionsRepo
    {
        private static Dictionary<int, List<Transaction>> _transactionsDictionary;
        private static IEnumerable<Transaction> _transactions;

        private static bool _isInitialized;

        public static void Initialize(IEnumerable<Transaction> transactions)
        {
            _transactions = transactions;
            _transactionsDictionary = _transactions.ToDictionaryList(x => x.SecurityId);
            _isInitialized = true;
        }


        public static IEnumerable<Transaction> GetTransactions(DateTime asof, int securityId)
        {
            if (!_isInitialized)
                throw new ArgumentException("Bitte vorher das Repo initialiseren");

            if (!_transactionsDictionary.TryGetValue(securityId, out var transactions))
                throw new ArgumentException("Es wurden keine Transacktionen gefunden!");


            return transactions/*.Where(t => t.TransactionDateTime <= asof)*/.OrderBy(x => x.TransactionDateTime);

            //sortiere die Transactionen hier mit dem frühesten DateTime bgeinnend
            var orderdReversed = transactions.Where(t => t.TransactionDateTime <= asof).OrderBy(x => x.TransactionDateTime).ToList();
            //danach laufe ich sie von hinten durch bis zum ersten opening
            //und gebe nur diese Range zurück
            var index = 0;
            for (var i = orderdReversed.Count - 1; i >= 0; i--)
            {
                var current = orderdReversed[i];
                index = i;
                if (current.TransactionType == TransactionType.Open)
                    break;
            }

            return orderdReversed.GetRange(index, orderdReversed.Count - 1 - index);
        }

        public static IEnumerable<Transaction> GetAllTransactions()
        {
            return _transactions;
        }

        public static int GetAllTransactionsCount()
        {
            return _transactions.Count();
        }
    }


    public static class StoppLossRepo
    {
        private static IEnumerable<Transaction> _stopps;
        private static bool _isInitialized;

        public static void Initialize(IEnumerable<Transaction> stopps)
        {
            _stopps = stopps;
            _isInitialized = true;
        }


        public static IEnumerable<Transaction> GetStops(DateTime asof)
        {
            if (!_isInitialized)
                throw new ArgumentException("Bitte vorher das Repo initialiseren");
            return _stopps.Where(x => x.TransactionDateTime <= asof);
        }
    }

    public class TradingViewModel : INotifyPropertyChanged, ISmartProgessRegion
    {
        #region Private Members

        private PortfolioManager _portfolioManager;
        private readonly IScoringProvider _scoringProvider;
        private IEnumerable<TransactionViewModel> _holdings;
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
        }

        private async void ShowSelectedCandidateWindow()
        {
            if (SelectedCandidate == null)
                return;

            if (!_scoringProvider.PriceHistoryStorage.TryGetValue(SelectedCandidate.Record.SecurityId, out var priceHistoryCollection))
                return;

            var win = new ChartWindow(SelectedCandidate.Record.SecurityId, ChartDate);

            await win.CreateFints(priceHistoryCollection, NameCatalog[SelectedCandidate.Record.SecurityId]);

            win.Show();
        }

        private async void ShowNewSelectedPositionWindow()
        {
            if (SelectedPosition == null)
                return;

            if (!_scoringProvider.PriceHistoryStorage.TryGetValue(SelectedPosition.SecurityId, out var priceHistoryCollection))
                return;

            var win = new ChartWindow(SelectedPosition.SecurityId, ChartDate);

            await win.CreateFints(priceHistoryCollection, NameCatalog[SelectedPosition.SecurityId]);

            win.Show();

        }

        public TradingViewModel(List<ITransaction> transactions, IScoringProvider scoringProvider) : this()
        {
            _scoringProvider = scoringProvider;
            Init(transactions);
        }

        public TradingViewModel(ScoringProvider scoringProvider) : this()
        {
            _scoringProvider = scoringProvider;
        }

        #endregion

        #region Events

        public event EventHandler<IndexBacktestResultEventArgs> IndexBacktestCompletedEvent;

        public event EventHandler<BacktestResultEventArgs> BacktestCompletedEvent;

        public event EventHandler<DayOfWeek> MoveCursorToNextTradingDayEvent;

        public event EventHandler MoveCursorToNextStoppDayEvent;

        #endregion

        #region Commands

        public ICommand RunNewIndexBacktestCommand { get; }

        public ICommand RunNewBacktestCommand { get; }

        public ICommand LoadBacktestCommand { get; }

        public ICommand MoveCursorToNextTradingDayCommand { get; }

        public ICommand MoveCursorToNextStoppDayCommand { get; }

        public ICommand ShowSelectedPositionCommand { get; }

        public ICommand ShowSelectedCandidateCommand { get; }




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
                var exposureWatcher = new ExposureWatcher(indexOutput);
                var backtestHandler = new BacktestHandler(exposureWatcher);
                _cancellationSource = new CancellationTokenSource();
                await backtestHandler.RunIndexBacktest(StartDateTime, EndDateTime, _cancellationSource.Token);

                IndexBacktestCompletedEvent?.Invoke(this, new IndexBacktestResultEventArgs(backtestHandler.IndexResults.CastToList<IIndexBackTestResult>()));
            }
        }

        private async void OnRunBacktest()
        {

            //Pm erstellen für den Backtest
            using (SmartBusyRegion.Start(this))
            {
                //Clean up
                var files = Directory.GetFiles(Settings.LoggingPath);
                foreach (var file in files)
                    File.Delete(file);

                var transactionsPath = Path.Combine(Settings.LoggingPath, "Transactions.csv");

                var pm = new PortfolioManager(null, Settings, new TransactionsHandler(null, new BacktestTransactionsCacheProvider(() => LoadHistory(transactionsPath))));

                //scoring Provider registrieren
                pm.RegisterScoringProvider(_scoringProvider);
                pm.CalcStartingAllocationToRisk(StartDateTime);

                var loggingProvider = new LoggingSaveProvider(Settings.LoggingPath, pm);

                //einen BacktestHandler erstellen
                _candidatesProvider = new CandidatesProvider(_scoringProvider);

                //backtestHandler erstellen
                var backtestHandler = new BacktestHandler(pm, _candidatesProvider, loggingProvider);

                //Backtest
                _cancellationSource = new CancellationTokenSource();
                await backtestHandler.RunBacktest(StartDateTime, EndDateTime, _cancellationSource.Token);
                _portfolioManager = pm;


            }

            //create output
            var valuations = SimpleTextParser.GetListOfTypeFromFilePath<PortfolioValuation>(Path.Combine(Settings.LoggingPath, "PortfolioValuations.csv"));
            var cashMovements = SimpleTextParser.GetListOfTypeFromFilePath<CashMetaInfo>(Path.Combine(Settings.LoggingPath, nameof(CashMetaInfo) + "s.csv"));

            //Repos initialisieren
            TransactionsRepo.Initialize(SimpleTextParser.GetListOfTypeFromFilePath<Transaction>(Path.Combine(Settings.LoggingPath, "Transactions.csv")));
            StoppLossRepo.Initialize(SimpleTextParser.GetListOfTypeFromFilePath<Transaction>(Path.Combine(Settings.LoggingPath, "StoppLoss" + nameof(Transaction) + "s.csv")));

            if (_cashInfosDictionary?.Count > 0)
                _cashInfosDictionary.Clear();
            //CashInfos updaten
            _cashInfosDictionary = new CashInfoCollection(cashMovements);

            TransactionsCountTotal = TransactionsRepo.GetAllTransactionsCount();

            AveragePortfolioSize = await CalculateAveragePortfolioHoldings();

            PortfolioTurnOver = TransactionsCountPerWeek / AveragePortfolioSize;

            var resultWindow = new ResultWindow { DataContext = this };
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(valuations, TransactionsRepo.GetAllTransactions(), Settings));
            resultWindow.Show();
        }



        private Task<decimal> CalculateAveragePortfolioHoldings()
        {
            return Task.Run(() =>
            {
                var count = 0;
                var sum = 1M;
                foreach (var dateGrp in TransactionsRepo.GetAllTransactions().GroupBy(x => x.TransactionDateTime))
                {
                    var holdings = _portfolioManager.TransactionsHandler.GetCurrentHoldings(dateGrp.Key);
                    count++;
                    sum += holdings.Count();
                }

                return sum/count;
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

        private void OnLoadBacktest()
        {
            var filePath = _portfolioManager.PortfolioSettings.LoggingPath;
            var values = SimpleTextParser.GetListOfType<PortfolioValuation>(File.ReadAllText(filePath));
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(values, null, Settings));
        }

        #endregion

        #region Helpers

        public static Dictionary<int, string> NameCatalog => _nameCatalog ?? CreateCatalog();

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
        private IEnumerable<ITradingCandidateBase> _candidates;
        private CandidatesProvider _candidatesProvider;
        private List<Transaction> _transactions;
        private TransactionViewModel _selectedPosition;
        private ITradingCandidateBase _selectedCandidate;
        private double _value;
        private double _maximum;
        private int _transactionsCountTotal;
        private decimal _portfolioTurnOver;
        private decimal _averagePortfolioSize;
        private static Dictionary<int, string> _nameCatalog;

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
            Stopps = StoppLossRepo.GetStops(asof).Select(x => new TransactionViewModel(x, GetScore(x, x.TransactionDateTime), true));
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
                Holdings = _portfolioManager.TransactionsHandler.GetCurrentHoldings(asof).Select(t => new TransactionViewModel(t, GetScore(t, asof)));
            else
            {
                //ich returne am tading tag den portfoliostand vor der umschichtung + die umschichtungen separat, damit
                //die anzeige in der Gui klarer ist und nachvollzogen werden kann, was zu dem Stichtag geschehen ist
                Holdings = _portfolioManager.TransactionsHandler.GetCurrentHoldings(asof.AddDays(-1)).Select(t => new TransactionViewModel(t, GetScore(t, asof)))
                    .Concat(tradingDayTransaction.Select(t => new TransactionViewModel(t, GetScore(t, asof)) { IsNew = true }));
            }
        }

        private void UpdateCandidates(DateTime asof)
        {
            Candidates = _candidatesProvider.GetCandidates(asof);
        }

        private IScoringResult GetScore(ITransaction transaction, DateTime asof)
        {
            return _scoringProvider.GetScore(transaction.SecurityId, asof);
        }


        #endregion

        #region Public Members

        public ITradingCandidateBase SelectedCandidate
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

        public IEnumerable<ITradingCandidateBase> Candidates
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
        public IEnumerable<TransactionViewModel> Holdings
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

        public double Value
        {
            get => _value;
            set
            {
                if (value.Equals(_value))
                    return;
                _value = value;
                OnPropertyChanged();
                ProgressValueChanged?.Invoke(this, Value);
            }
        }

        public double Maximum
        {
            get => _maximum;
            set
            {
                if (value.Equals(_maximum))
                    return;
                _maximum = value;
                OnPropertyChanged();
                MaximumValueChanged?.Invoke(this, Maximum);
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

        public event EventHandler<double> ProgressValueChanged;
        public event EventHandler<double> MaximumValueChanged;
    }
}