using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Lib.UI.WPF.Core.Controls.Core;
using Common.Lib.UI.WPF.Core.Input;
using Common.Lib.UI.WPF.Core.Primitives;
using HelperLibrary.Database.Models;
using HelperLibrary.Extensions;
using HelperLibrary.Parsing;
using HelperLibrary.Trading;
using HelperLibrary.Trading.PortfolioManager;
using JetBrains.Annotations;
using Microsoft.Win32;
using Trading.DataStructures.Interfaces;
using Trading.UI.Wpf.Models;
using Trading.UI.Wpf.Utils;

namespace Trading.UI.Wpf.ViewModels
{

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

        #endregion

        #region Constructor

        public TradingViewModel()
        {
            Settings = new SettingsViewModel(new ConservativePortfolioSettings());

            StartDateTime = new DateTime(2000, 01, 01);
            EndDateTime = StartDateTime.AddYears(5);

            //Command
            RunNewBacktestCommand = new RelayCommand(OnRunBacktest);
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


        public event EventHandler<BacktestResultEventArgs> BacktestCompletedEvent;

        public event EventHandler<DayOfWeek> MoveCursorToNextTradingDayEvent;

        #endregion

        #region Commands


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
            var cashMovements = SimpleTextParser.GetListOfTypeFromFilePath<PortfolioValuation>(Path.Combine(Settings.LoggingPath, "PortfolioValue.csv"));

            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(valuations,transactions));
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
            BacktestCompletedEvent?.Invoke(this, new BacktestResultEventArgs(values,null));
        }

        #endregion

        #region Helpers

        public static Dictionary<int, string> NameCatalog => Factory.GetIdToNameDictionary();

        public void UpdateHoldings(DateTime asof, bool isTradingDay = false)
        {
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

        public SettingsViewModel Settings { get; }


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


}