using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Database.Models;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;

namespace TradingSystemTests.Models
{
    public class TestTransactionsHandler : ITransactionsHandler
    {
        #region private

        private readonly ITransactionsCacheProvider _cacheProvider;
        private IPortfolio _currentPortfolio;
        private DateTime? _lastAsOf;
        private IScoringProvider _scoringProvider;

        #endregion

        #region Constructor


        public TestTransactionsHandler(IEnumerable<TransactionItem> transactions)
        {
            _currentPortfolio = GetCurrentPortfolio(transactions);
        }

        public TestTransactionsHandler(ITransactionsCacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        public TestTransactionsHandler(IEnumerable<TransactionItem> transactions, ITransactionsCacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
            // wenn keine items usupllied werden dann gerife ich auf den Cache zu
            _currentPortfolio = GetCurrentPortfolio(transactions) ?? GetCurrentPortfolio(_cacheProvider?.TransactionsCache.Value.Values);

        }

        #endregion

        #region Public Properties

        public IPortfolio CurrentPortfolio => GetCurrentPortfolio();
    

        #region Index

        public IEnumerable<TransactionItem> this[TransactionType key]
        {
            get
            {
                var transactionsTypeDic = new Dictionary<int, List<TransactionItem>>();
                foreach (var items in _cacheProvider.TransactionsCache.Value.Values)
                {
                    foreach (var item in items)
                    {
                        if (!transactionsTypeDic.ContainsKey(item.TransactionType))
                            transactionsTypeDic.Add(item.TransactionType, new List<TransactionItem>());

                        transactionsTypeDic[item.TransactionType].Add(item);
                    }
                }

                return transactionsTypeDic.ContainsKey((int)key) ? transactionsTypeDic[(int)key] : null;
            }
        }

        public IEnumerable<TransactionItem> this[DateTime key]
        {
            get
            {
                var dateTimeDic = new Dictionary<DateTime, List<TransactionItem>>();
                foreach (var items in _cacheProvider.TransactionsCache.Value.Values)
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

        #endregion

        #endregion

        #region ITransactionHandler

        public decimal? GetWeight(int secid, DateTime? asof = null)
        {
            return asof == null
                ? CurrentPortfolio[secid]?.TargetWeight ?? -1
                : this[asof.Value]?.FirstOrDefault(x => x.SecurityId == secid)?.TargetWeight;
        }

        public bool? IsActiveInvestment(int secid, DateTime? asof = null)
        {
            return asof == null
                ? CurrentPortfolio[secid] != null
                : this[asof.Value]?.FirstOrDefault(x => x.SecurityId == secid) != null;
        }

        public decimal? GetPrice(int secId, DateTime? asof = null)
        {
            if (asof == null)
                return CurrentPortfolio[secId].TargetAmountEur / CurrentPortfolio[secId].Shares;

            var transaction = this[asof.Value]?.FirstOrDefault(x => x.SecurityId == secId);
            return transaction?.TargetAmountEur / (transaction?.Shares * -1);
        }

        public decimal? GetPrice(int secId, TransactionType transactionType)
        {
            var transaction = this[transactionType]?.OrderByDescending(x => x.TransactionDateTime)
                .FirstOrDefault(x => x.SecurityId == secId);
            return transaction?.TargetAmountEur / transaction?.Shares;
        }

        public decimal? GetAveragePrice(int secid, DateTime asof)
        {
            //bekomme hier schon die sortierten, aktiven Transaktionen einer SecurityId
            var pastTransactions = Get(secid, true).ToList();

            //einfachster Fall es gibt nur eine Transaktion (ein opening dann erhöhe ich die
            //summe um Shares * aktueller Preis
            if (pastTransactions.Count == 1 || pastTransactions[0].Shares < 0)
                return pastTransactions[0].EffectiveAmountEur/ pastTransactions[0].Shares;

            decimal averagePrice = 0;
            //sonst muss ich den mittleren Preis zurückgeben, da die Position ja aufgebaut / nachgekauft wurde
            foreach (var transaction in pastTransactions)
            {
                var price = _scoringProvider
                    .GetTradingRecord(transaction.SecurityId, transaction.TransactionDateTime)
                    .AdjustedPrice;

                averagePrice += (decimal)transaction.Shares / CurrentPortfolio[secid].Shares * price;
            }

            return averagePrice;
        }

        public TransactionItem GetSingle(int secId, TransactionType? transactionType, bool getLatest = true)
        {
            if (transactionType == null)
                return CurrentPortfolio?[secId];

            if (!getLatest)
                return this[transactionType.Value]?
                    .OrderBy(x => x.TransactionDateTime)
                    .FirstOrDefault(x => x.SecurityId == secId);

            return this[transactionType.Value]?
                .OrderByDescending(x => x.TransactionDateTime)
                .FirstOrDefault(x => x.SecurityId == secId);
        }

        /// <summary>
        /// DIe Methode gibt alle Transaktionen zurück zu einer Secid
        /// </summary>
        /// <param name="secId">die SecId</param>
        /// <param name="activeOnly">ddas Falg das angibt ob nur offenen transaktonen zurückgegeben werden sollen</param>
        /// <param name="filter">der optionale Filter</param>
        /// <returns></returns>
        public IEnumerable<TransactionItem> Get(int secId, bool activeOnly = false, Predicate<TransactionItem> filter = null)
        {
            if (!_cacheProvider.TransactionsCache.Value.TryGetValue(secId, out var transactionItems))
                return null;

            var transactionsSorted = transactionItems
                .OrderByDescending(x => x.TransactionDateTime)
                .ToList();

            if (activeOnly && transactionsSorted.Count > 1)
            {
                var idx = 0;
                var closeIdx = 0;
                //Solange ich nicht bei der eröffnungsposition bin gehe ich weiter zurück
                while (idx < transactionsSorted.Count)
                {   //der TransactionType wird als int gestored im Model
                    if ((TransactionType)transactionsSorted[idx].TransactionType ==
                        TransactionType.Open)
                        break;

                    if ((TransactionType)transactionsSorted[idx].TransactionType ==
                        TransactionType.Close)
                        closeIdx = idx;

                    //den index erhöhen
                    idx++;
                }
                //ich will die close position nicht mehr dabei haben, sprich ich gehe vom open bis zum letzten change
                return closeIdx == 0
                    ? transactionsSorted.Where((t, i) => i <= idx)
                    : transactionsSorted.Where((t, i) => i <= idx && i > closeIdx);
            }

            return filter == null
                ? transactionsSorted
                : transactionsSorted.Where(x => filter(x));
        }

        #endregion

        #region Get Current Portfolio


        private IPortfolio GetCurrentPortfolio(IEnumerable<List<TransactionItem>> cacheItems)
        {
            //flatten the cacheItems
            var items = new List<TransactionItem>();
            foreach (var item in cacheItems)
                items.AddRange(item);

            //merke mir hier das letzte asof
            var currentAsof = items.OrderByDescending(x => x.TransactionDateTime).FirstOrDefault()?.TransactionDateTime;
            if (_lastAsOf != null && currentAsof <= _lastAsOf)
                return _currentPortfolio;

            //sonst muss ich es neu rechnen
            _lastAsOf = currentAsof;

            return GetCurrentPortfolio(items);
        }


        public IPortfolio GetCurrentPortfolio()
        {
            //Sollte nur beim Testen der Fall sein
            if (_cacheProvider == null)
                return _currentPortfolio;

            return GetCurrentPortfolio(_cacheProvider.TransactionsCache.Value.Values);
        }

        private IPortfolio GetCurrentPortfolio(IEnumerable<TransactionItem> transactionItems)
        {
            if (transactionItems == null)
                return null;

            //temporäres dictionary erstellen
            var dic = new Dictionary<int, TransactionItem>();

            //gruppuieren nach SecID => wenn der Count genau 1 ist kann ich sie einfach adden sonst muss ich summieren
            foreach (var secIdGrp in transactionItems.GroupBy(x => x.SecurityId))
            {
                if (secIdGrp.Count() == 1)
                    dic.Add(secIdGrp.Key, secIdGrp.First());
                else
                {
                    //wenn aktuellste Eintrag ein Close ist, kann ich die Transaktion ignorieren
                    if (secIdGrp.OrderByDescending(x => x.TransactionDateTime).FirstOrDefault()?.TransactionType == (int)TransactionType.Close)
                        continue;

                    //sumItem erstellen
                    var sumItem = new TransactionItem();

                    // nur nicht gecancellte Transaktionen berücksichtigen
                    foreach (var item in secIdGrp.Where(x => x.Cancelled != 1))
                    {
                        sumItem.SecurityId = secIdGrp.Key;
                        sumItem.Shares += item.Shares;
                        sumItem.TargetAmountEur += item.Shares < 0 ? -item.TargetAmountEur : item.TargetAmountEur;
                        //Target Weight braucht nicht aufsummiert werden
                        sumItem.TargetWeight = item.TargetWeight;

                        sumItem.EffectiveWeight += item.EffectiveWeight;
                        sumItem.EffectiveAmountEur += item.EffectiveAmountEur;
                        sumItem.TransactionDateTime = item.TransactionDateTime;
                        sumItem.TransactionType = item.TransactionType;
                    }

                    dic.Add(sumItem.SecurityId, sumItem);
                }
            }

            return (_currentPortfolio = new TestPortfolio(dic.Values, _lastAsOf));
        }

        #endregion

        #region Update

        public void UpdateCache()
        {
            _cacheProvider.UpdateCache();
        }

        public void RegisterScoringProvider(IScoringProvider scoringProvider)
        {
            _scoringProvider = scoringProvider;
        }

        #endregion
    }
}