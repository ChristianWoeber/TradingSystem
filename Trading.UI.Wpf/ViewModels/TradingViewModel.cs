using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Common.Lib.UI.WPF.Core.Input;
using HelperLibrary.Database.Models;
using HelperLibrary.Interfaces;
using HelperLibrary.Parsing;
using HelperLibrary.Trading.PortfolioManager;
using JetBrains.Annotations;
using Trading.DataStructures.Interfaces;
using Trading.UI.Wpf.Models;
using Trading.UI.Wpf.Utils;

namespace Trading.UI.Wpf.ViewModels
{

    public class BacktestResultEventArgs
    {
        public List<PortfolioValuation> PortfolioValuations { get; }

        public BacktestResultEventArgs(List<PortfolioValuation> portfolioValuations)
        {
            PortfolioValuations = portfolioValuations;
        }
    }

    public class TradingViewModel : INotifyPropertyChanged
    {
        private PortfolioManager _portfolioManager;
        private readonly IScoringProvider _scoringProvider;
        private IEnumerable<TransactionViewModel> _holdings;

        public TradingViewModel(List<ITransaction> transactions, IScoringProvider scoringProvider)
        {
            _scoringProvider = scoringProvider;
            Init(transactions);

            //Command
            RunBacktestCommand = new RelayCommand(RunBacktest);
            MoveCursorToNextTradingDayCommand = new RelayCommand(() => MoveCursorToNextTradingDay?.Invoke(this, _portfolioManager.PortfolioSettings.TradingDay));
        }


        public event EventHandler<BacktestResultEventArgs> BacktestCompleted;

        public event EventHandler<DayOfWeek> MoveCursorToNextTradingDay;

        public ICommand RunBacktestCommand { get; }

        public ICommand MoveCursorToNextTradingDayCommand { get; }

        private void Init(List<ITransaction> transactions)
        {
            _portfolioManager = new PortfolioManager(null
                , new ConservativePortfolioSettings { LoggingPath = Globals.PortfolioValuePath }
                , new TransactionsHandler(null, new BacktestTransactionsCacheProvider(transactions)));

            //scoring Provider registrieren
            _portfolioManager.RegisterScoringProvider(_scoringProvider);

            //BacktestCompleted Event feuern
            BacktestCompleted?.Invoke(this, new BacktestResultEventArgs(SimpleTextParser.GetListOfType<PortfolioValuation>(Path.Combine(_portfolioManager.PortfolioSettings.LoggingPath, "PortfolioValue"))));

        }

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

        private void RunBacktest()
        {
            var filePath = _portfolioManager.PortfolioSettings.LoggingPath;
            var values = SimpleTextParser.GetListOfType<PortfolioValuation>(File.ReadAllText(filePath));
            BacktestCompleted?.Invoke(this, new BacktestResultEventArgs(values));
        }

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