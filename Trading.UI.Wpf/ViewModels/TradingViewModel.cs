using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using Common.Lib.Extensions;
using Common.Lib.UI.WPF.Core.Controls.Core;
using Common.Lib.UI.WPF.Core.Input;
using Common.Lib.UI.WPF.Core.Primitives;
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
using Trading.DataStructures.Interfaces;
using Trading.UI.Wpf.Models;
using Trading.UI.Wpf.Utils;

namespace Trading.UI.Wpf.ViewModels
{
    public class IndexBacktestResultEventArgs
    {
        public List<IIndexBackTestResult> Results { get; }

        public IndexBacktestResultEventArgs(List<IIndexBackTestResult> results)
        {
            Results = results;
        }
    }


    public class BacktestResultEventArgs
    {
        public List<PortfolioValuation> PortfolioValuations { get; }

        //public ICashCollection CashMovements { get; }

        public List<Transaction> Transactions { get; }

        public BacktestResultEventArgs(List<PortfolioValuation> portfolioValuations, List<Transaction> transactions)
        {
            PortfolioValuations = portfolioValuations;
            Transactions = transactions;
        }
    }

    public class TradingViewModel : INotifyPropertyChanged, ISmartBusyRegion
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

        #endregion

        #region Commands

        public ICommand RunNewIndexBacktestCommand { get; }

        public ICommand RunNewBacktestCommand { get; }

        public ICommand LoadBacktestCommand { get; }

        public ICommand MoveCursorToNextTradingDayCommand { get; }


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
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(portfolioValuation, null));
        }


        #endregion

        #region CommandActions

        private async void OnRunIndexBacktest()
        {
            using (SmartBusyRegion.Start(this))
            {
                var indexOutput = new IndexResult { IndicesDirectory = Globals.IndicesBasePath };
                var exposureWatcher = new ExposureWatcher(indexOutput, IndexSettings.TypeOfIndex);
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
                var candidatesProvider = new CandidatesProvider(_scoringProvider);

                //backtestHandler erstellen
                var backtestHandler = new BacktestHandler(pm, candidatesProvider, loggingProvider);

                //Backtest
                _cancellationSource = new CancellationTokenSource();
                await backtestHandler.RunBacktest(StartDateTime, EndDateTime, _cancellationSource.Token);
                _portfolioManager = pm;
            }

            //create output
            var valuations = SimpleTextParser.GetListOfTypeFromFilePath<PortfolioValuation>(Path.Combine(Settings.LoggingPath, "PortfolioValuations.csv"));
            var transactions = SimpleTextParser.GetListOfTypeFromFilePath<Transaction>(Path.Combine(Settings.LoggingPath, "Transactions.csv"));
            var cashMovements = SimpleTextParser.GetListOfTypeFromFilePath<CashMetaInfo>(Path.Combine(Settings.LoggingPath, nameof(CashMetaInfo) + "s.csv"));

            if (_cashInfosDictionary?.Count > 0)
                _cashInfosDictionary.Clear();
            //CashInfos updaten
            _cashInfosDictionary = new CashInfoCollection(cashMovements);
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(valuations, transactions));
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
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(values, null));
        }

        #endregion

        #region Helpers

        public static Dictionary<int, string> NameCatalog => Factory.GetIdToNameDictionary();

       
        private IEnumerable<CashMetaInfo> _cash;

        public void UpdateCash(DateTime toDateTime)
        {
            if (_portfolioManager == null)
                return;

            if (_cashInfosDictionary.TryGetLastCash(toDateTime, out var infos))
                Cash = infos;
        }


        public void UpdateHoldings(DateTime asof, bool isTradingDay = false)
        {
            if (_portfolioManager == null)
                return;

            UpdateCash(asof);
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

        private IScoringResult GetScore(ITransaction transaction, DateTime asof)
        {
            return _scoringProvider.GetScore(transaction.SecurityId, asof);
        }


        #endregion

        #region Public Members


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

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion


    }

    public class CashInfoCollection : Dictionary<DateTime, List<CashMetaInfo>>
    {
        private readonly int _maxTries;


        public CashInfoCollection(List<CashMetaInfo> cashMovements, int maxTries = 15)
        {
            _maxTries = maxTries;
            foreach (var cash in cashMovements)
            {
                if (!TryGetValue(cash.Asof, out var _))
                    Add(cash.Asof, new List<CashMetaInfo>());
                this[cash.Asof].Add(cash);
            }
        }

     
        public bool TryGetLastCash(DateTime key, out List<CashMetaInfo> cashMetaInfos)
        {
            if (TryGetValue(key, out var infos))
            {
                cashMetaInfos = infos;
                return true;
            }

            var count = 0;
            var date = key;

            while (count < _maxTries)
            {
                count++;
                if (TryGetValue(date.AddDays(-count), out var match))
                {
                    cashMetaInfos = match;
                    return true;
                }

            }
            cashMetaInfos = new List<CashMetaInfo>();
            return false;
        }
    }
}