﻿using System;
using System.Collections.Generic;
using Trading.DataStructures.Interfaces;

namespace TradingSystemTests.Models
{
    public class TransactionsCacheProviderTest : ITransactionsCacheProvider
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

        public TransactionsCacheProviderTest(Func<Dictionary<int, List<ITransaction>>> loadFunc)
        {
            _loadFunc = loadFunc;
            TransactionsCache = new Lazy<Dictionary<int, List<ITransaction>>>(loadFunc);
        }
    }
}