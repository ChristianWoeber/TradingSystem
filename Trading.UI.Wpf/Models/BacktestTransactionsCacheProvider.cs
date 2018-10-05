using System;
using System.Collections.Generic;
using HelperLibrary.Extensions;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.Models
{
    public class BacktestTransactionsCacheProvider : ITransactionsCacheProvider
    {      
        public void UpdateCache()
        {
           //empty in dem Fall
        }

        public Lazy<Dictionary<int, List<ITransaction>>> TransactionsCache { get; }

        public Dictionary<DateTime, List<ITransaction>> DateTimeTransactionsCache { get; }

        public BacktestTransactionsCacheProvider(List<ITransaction> transactions)
        {
            DateTimeTransactionsCache = transactions.ToDictionaryList(x => x.TransactionDateTime);
            TransactionsCache = new Lazy<Dictionary<int, List<ITransaction>>>(()=> transactions.ToDictionaryList(x=>x.SecurityId));
        }
      
    }

}
