using System;
using System.Collections.Generic;
using HelperLibrary.Extensions;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.Models
{
    public class BacktestTransactionsCacheProvider : ITransactionsCacheProvider
    {
        private readonly Func<Dictionary<int, List<ITransaction>>> _loadFunc;

        private readonly HashSet<string> _uniqueTransactionsKeySet = new HashSet<string>();


        public void UpdateCache()
        {
            foreach (var dicEntry in _loadFunc.Invoke())
            {
                if (!TransactionsCache.Value.ContainsKey(dicEntry.Key))
                    TransactionsCache.Value.Add(dicEntry.Key, new List<ITransaction>());

                //sonst Werte einfügen
                foreach (var item in dicEntry.Value)
                {
                    //Wenn es die transaktion schon gibt weiter
                    if (_uniqueTransactionsKeySet.Contains(item.UniqueKey))
                        continue;
                    //Unique Cache added
                    _uniqueTransactionsKeySet.Add(item.UniqueKey);
                    TransactionsCache.Value[dicEntry.Key].Add(item);
                }
            }
        }

        public Lazy<Dictionary<int, List<ITransaction>>> TransactionsCache { get; }

        public Dictionary<DateTime, List<ITransaction>> DateTimeTransactionsCache { get; }

        /// <summary>
        /// Der Konstruktor wird aufgerufen wenn ich schon die Transaktionen hab
        /// </summary>
        /// <param name="transactions"></param>
        public BacktestTransactionsCacheProvider(List<ITransaction> transactions)
        {
            DateTimeTransactionsCache = transactions.ToDictionaryList(x => x.TransactionDateTime);
            TransactionsCache = new Lazy<Dictionary<int, List<ITransaction>>>(()=> transactions.ToDictionaryList(x=>x.SecurityId));
        }

        /// <summary>
        /// Der Konstruktor wird für das konkrete Backtesten verwendet
        /// </summary>
        /// <param name="loadFunc"></param>
        public BacktestTransactionsCacheProvider(Func<Dictionary<int, List<ITransaction>>> loadFunc)
        {
            _loadFunc = loadFunc;
            TransactionsCache = new Lazy<Dictionary<int, List<ITransaction>>>(loadFunc);
        }
    }

}
