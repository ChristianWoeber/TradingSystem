using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Database.Models;
using HelperLibrary.Extensions;
using Trading.DataStructures.Enums;

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
}