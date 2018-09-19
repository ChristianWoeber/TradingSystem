﻿using HelperLibrary.Database;
using HelperLibrary.Database.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    /// <summary>
    /// Hilfsklasse für das Schreiben und lesen der kompletten Transaktionen
    /// </summary>
    public class TransactionsWrapper : IEnumerable<TransactionItem>, ITransactionsHandler
    {
        #region private


        /// <summary>
        /// The Interface that provides the Transactionscache
        /// </summary>
        private readonly ITransactionsCacheProvider _transactionsCacheProvider;


        #endregion 

        #region Constructor

        public TransactionsWrapper(ITransactionsCacheProvider transactionsCacheProvider = null)
        {
            //Standardfall ist ein DatenbankProvider
            _transactionsCacheProvider = transactionsCacheProvider ?? new DatabaseTransactionsProvider();
        }


        #endregion

        #region IPortfolio

        /// <summary>
        /// The Current Portfolio-Holdings
        /// </summary>
        public IPortfolio CurrentPortfolio { get; } = new Portfolio();

        #endregion

        #region Index

        public IEnumerable<TransactionItem> this[DateTime key]
        {
            get
            {
                var dateTimeDic = new Dictionary<DateTime, List<TransactionItem>>();
                foreach (var items in _transactionsCacheProvider.TransactionsCache.Value.Values)
                {
                    foreach (var item in items)
                    {
                        if (!dateTimeDic.ContainsKey(item.TransactionDateTime))
                            dateTimeDic.Add(item.TransactionDateTime, new List<TransactionItem>());

                        dateTimeDic[item.TransactionDateTime].Add(item);
                    }
                }
                return dateTimeDic.ContainsKey(key) ? dateTimeDic[key] : null;
            }

        }


        public IEnumerable<TransactionItem> this[int key] => _transactionsCacheProvider.TransactionsCache.Value.ContainsKey(key)
            ? _transactionsCacheProvider.TransactionsCache.Value[key]
            : null;

        #endregion

        #region Helper Methods

        public decimal? GetWeight(int securityId, DateTime? asof = null)
        {
            return asof == null
                ? CurrentPortfolio[securityId].TargetWeight
                : this[asof.Value]?.FirstOrDefault(x => x.SecurityId == securityId)?.TargetWeight;
        }

        public bool? IsActiveInvestment(int securityId, DateTime? asof = null)
        {
            if (asof == null)
                return CurrentPortfolio[securityId] != null;

            return this[asof.Value]?.FirstOrDefault(x => x.SecurityId == securityId) != null;
        }

        public decimal? GetPrice(int secid, DateTime? asof = null)
        {
            if (asof == null)
                return CurrentPortfolio[secid]?.TargetAmountEur / CurrentPortfolio[secid]?.Shares;

            var transaction = this[asof.Value]?.FirstOrDefault(x => x.SecurityId == secid);
            return transaction?.TargetAmountEur / transaction?.Shares;

        }

        public decimal? GetPrice(int secid, TransactionType transactionType)
        {
            throw new NotImplementedException();
        }

        public decimal? GetAveragePrice(int secid, DateTime asof)
        {
            throw new NotImplementedException();
        }

        public TransactionItem GetSingle(int secId, TransactionType? transactionType, bool getLatest = true)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TransactionItem> Get(int secId, bool activeOnly = false, Predicate<TransactionItem> filter = null)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Enumerator

        /// <summary>
        /// Ich gebe immer die kompletten Transaktionen, sortiert nach Datum zurück
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TransactionItem> GetEnumerator()
        {
            var ls = new List<TransactionItem>();
            foreach (var listValue in _transactionsCacheProvider.TransactionsCache.Value.Values)
                ls.AddRange(listValue);

            return ls.OrderBy(x => x.TransactionDateTime).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void UpdateCache()
        {
            throw new NotImplementedException();
        }

        public void RegisterScoringProvider(IScoringProvider scoringProvider)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region NestedClasses

        internal class DatabaseTransactionsProvider : ITransactionsCacheProvider
        {
            public DatabaseTransactionsProvider()
            {
                TransactionsCache = new Lazy<Dictionary<int, List<TransactionItem>>>(LoadData);
            }

            private Dictionary<int, List<TransactionItem>> LoadData()
            {
                throw new NotImplementedException();
            }

            public Lazy<Dictionary<int, List<TransactionItem>>> TransactionsCache { get; }
            public void UpdateCache()
            {
                throw new NotImplementedException();
            }
        }

        internal class Portfolio : IPortfolio
        {
            private readonly List<TransactionItem> _currentPortfolio;
            private readonly DateTime? _lastAsOf;

            public Portfolio()
            {
                _currentPortfolio = new List<TransactionItem>(DataBaseQueryHelper.GetCurrentPortfolio());
                _lastAsOf = _currentPortfolio.OrderByDescending(x => x.TransactionDateTime).FirstOrDefault()?
                    .TransactionDateTime;

            }

            public TransactionItem this[int key]
            {
                get
                {
                    if (_currentPortfolio == null)
                        return null;

                    var dic = _currentPortfolio.ToDictionary(x => x.SecurityId);
                    return dic.ContainsKey(key) ? dic[key] : null;
                }
            }

            public bool HasItems(DateTime asof)
            {
                //Dann erstelle ich es aufjedenfall neu
                if (asof > _lastAsOf)
                    return false;
                //sonst nur wenn es noch keine Einträge hat
                return _currentPortfolio.Count > 0;
            }

            public bool IsInitialized => _lastAsOf != null;

            public IEnumerator<TransactionItem> GetEnumerator()
            {
                return _currentPortfolio.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion
    }
}
