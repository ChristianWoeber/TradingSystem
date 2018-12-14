using System.Collections.Generic;
using HelperLibrary.Database.Models;

namespace Trading.UI.Wpf.ViewModels.EventArgs
{
    public class BacktestResultEventArgs
    {
        public List<PortfolioValuation> PortfolioValuations { get; }

        //public ICashCollection CashMovements { get; }

        public IEnumerable<Transaction> Transactions { get; }

        public BacktestResultEventArgs(List<PortfolioValuation> portfolioValuations, IEnumerable<Transaction> transactions)
        {
            PortfolioValuations = portfolioValuations;
            Transactions = transactions;
        }
    }
}