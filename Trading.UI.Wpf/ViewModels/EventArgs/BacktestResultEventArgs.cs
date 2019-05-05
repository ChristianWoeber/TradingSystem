using System.Collections.Generic;
using HelperLibrary.Database.Models;

namespace Trading.UI.Wpf.ViewModels.EventArgs
{
    public class BacktestResultEventArgs
    {
        /// <summary>
        /// die gesamten Valuations
        /// </summary>
        public List<PortfolioValuation> PortfolioValuations { get; }

        //public ICashCollection CashMovements { get; }

        /// <summary>
        /// die gesamten Transaktionen
        /// </summary>
        public IEnumerable<Transaction> Transactions { get; }

        /// <summary>
        /// die Settings des Backtests
        /// </summary>
        public SettingsViewModel Settings { get; }

        public BacktestResultEventArgs(List<PortfolioValuation> portfolioValuations,
            IEnumerable<Transaction> transactions, SettingsViewModel settings)
        {
            PortfolioValuations = portfolioValuations;
            Transactions = transactions;
            Settings = settings;
        }
    }
}