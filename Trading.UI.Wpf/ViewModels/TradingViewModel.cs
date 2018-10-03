using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Common.Lib.UI.WPF.Core.Input;
using HelperLibrary.Database.Models;
using HelperLibrary.Interfaces;
using HelperLibrary.Parsing;
using HelperLibrary.Trading.PortfolioManager;
using JetBrains.Annotations;
using Trading.UI.Wpf.Models;

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
        private IEnumerable<Transaction> _holdings;

        public TradingViewModel(List<Transaction> transactions, IScoringProvider scoringProvider)
        {
            _scoringProvider = scoringProvider;
            Init(transactions);

            //Command
            RunBacktestCommand = new RelayCommand(RunBacktest);
        }


        public event EventHandler<BacktestResultEventArgs> BacktestCompleted;

        public ICommand RunBacktestCommand { get; }

        private void Init(List<Transaction> transactions)
        {
            _portfolioManager = new PortfolioManager(null
                , new ConservativePortfolioSettings { LoggingPath = Globals.PortfolioValuePath }
                , new TransactionsHandler(null, new TransactionsCacheProviderTest(transactions)));

            //scoring Provider registrieren
            _portfolioManager.RegisterScoringProvider(_scoringProvider);

            //BacktestCompleted Event feuern
            BacktestCompleted?.Invoke(this, new BacktestResultEventArgs(SimpleTextParser.GetListOfType<PortfolioValuation>(Path.Combine(_portfolioManager.PortfolioSettings.LoggingPath, "PortfolioValue"))));

        }



        public void UpdateHoldings(DateTime asof)
        {            
            Holdings = _portfolioManager.TransactionsHandler.GetCurrentHoldings(asof);
        }

        public IEnumerable<Transaction> Holdings
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


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}