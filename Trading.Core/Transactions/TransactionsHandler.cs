using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Trading.Core.Extensions;
using Trading.Core.Models;
using Trading.Core.Portfolio;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Transactions
{
    public class TransactionsHandler : ITransactionsHandler
    {
        #region private

        private readonly ITransactionsCacheProvider _cacheProvider;
        [JsonProperty]
        private IPortfolio _currentPortfolio;
        private DateTime? _lastAsOf;
        private IScoringProvider _scoringProvider;

        #endregion

        #region Constructor

        /// <summary>
        /// Wird für Deserialisierung gebraucht
        /// </summary>
        public TransactionsHandler()
        {

        }

        public TransactionsHandler(IEnumerable<ITransaction> transactions)
        {
            _currentPortfolio = GetCurrentPortfolio(transactions);
        }

        public TransactionsHandler(ITransactionsCacheProvider cacheProvider = null)
        {
            _cacheProvider = cacheProvider ?? throw new NotImplementedException("Achtung noch nicht implementiert");
        }

        public TransactionsHandler(IEnumerable<ITransaction> transactions, ITransactionsCacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
            // wenn keine items supllied werden dann greife ich auf den Cache zu
            _currentPortfolio = GetCurrentPortfolio(transactions) ?? GetCurrentPortfolio(_cacheProvider?.TransactionsCache.Value?.Values);

        }

        #endregion

        #region Public Properties

        public IPortfolio CurrentPortfolio => GetCurrentPortfolio();


        #region Index
        [JsonIgnore]
        public IEnumerable<ITransaction> this[TransactionType key]
        {
            get
            {
                var transactionsTypeDic = new Dictionary<int, List<ITransaction>>();
                foreach (var items in _cacheProvider.TransactionsCache.Value.Values)
                {
                    foreach (var item in items)
                    {
                        if (!transactionsTypeDic.ContainsKey((int)item.TransactionType))
                            transactionsTypeDic.Add((int)item.TransactionType, new List<ITransaction>());

                        transactionsTypeDic[(int)item.TransactionType].Add(item);
                    }
                }

                return transactionsTypeDic.ContainsKey((int)key) ? transactionsTypeDic[(int)key] : null;
            }
        }
        [JsonIgnore]
        public IEnumerable<ITransaction> this[DateTime key]
        {
            get
            {
                var dateTimeDic = new Dictionary<DateTime, List<ITransaction>>();
                foreach (var items in _cacheProvider.TransactionsCache.Value.Values)
                {
                    foreach (var item in items)
                    {
                        if (!dateTimeDic.ContainsKey(item.TransactionDateTime))
                            dateTimeDic.Add(item.TransactionDateTime, new List<ITransaction>());

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
                ? CurrentPortfolio[secid]?.TargetWeight
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

        //TODO: Average Price fixen
        public decimal? GetAveragePrice(int secid, DateTime asof)
        {
            if (CurrentPortfolio[secid] == null)
                return null;

            //bekomme hier schon die sortierten, aktiven Transaktionen einer SecurityId
            var pastTransactions = Get(secid, true)?.ToList();

            if (pastTransactions == null)
                return null;

            //einfachster Fall es gibt nur eine Transaktion (ein opening dann erhöhe ich die
            //summe um Shares * aktueller Preis
            if (pastTransactions.Count == 1 /*|| pastTransactions[0].Shares < 0*/)
                return pastTransactions[0].EffectiveAmountEur / pastTransactions[0].Shares;

        
            //var currentRecord = _scoringProvider.GetTradingRecord(secid, asof);
            return Math.Round(Math.Abs(CurrentPortfolio[secid].EffectiveAmountEur) / CurrentPortfolio[secid].Shares, 4);

            //var pastTransactionsOrdered = pastTransactions.OrderBy(x => x.TransactionDateTime).ToList();
            //decimal averagePrice = CurrentPortfolio[secid].EffectiveAmountEur / CurrentPortfolio[secid].Shares;

            //for (var i = 1; i < pastTransactionsOrdered.Count; i++)
            //{
            //    var runningShares = pastTransactions.GetRange(0, i + 1).Sum(x => x.Shares);
            //    averagePrice += CalcWeightedAverage(pastTransactions.GetRange(0, i + 1), CurrentPortfolio[secid].Shares);
            //}

            //return Math.Round(averagePrice, 4);
        }

        private decimal CalcWeightedAverage(List<ITransaction> range, decimal runningShares)
        {
            decimal average = 0;

            foreach (var transaction in range)
            {
                average += GetWeightedShares(transaction) * GetPrice(transaction);
            }

            return average;

            decimal GetWeightedShares(ITransaction transaction) => transaction.Shares / runningShares;

            decimal GetPrice(ITransaction transaction) => transaction.EffectiveAmountEur / transaction.Shares;
        }

        public ITransaction GetSingle(int secId, TransactionType? transactionType, bool getLatest = true)
        {
            if (transactionType == null && getLatest)
                return _cacheProvider.TransactionsCache.Value.TryGetValue(secId, out var transactionItems) ? transactionItems.OrderByDescending(x => x.TransactionDateTime).FirstOrDefault() : null;
            if (transactionType == null)
                return CurrentPortfolio[secId];
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
        public IEnumerable<ITransaction> Get(int secId, bool activeOnly = false, Predicate<ITransaction> filter = null)
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


        private IPortfolio GetCurrentPortfolio(IEnumerable<List<ITransaction>> cacheItems)
        {

            if (cacheItems == null)
                return null;

            //flatten the cacheItems
            var items = cacheItems.SelectMany(x => x).ToList();

            //merke mir hier das letzte asof
            var currentAsof = items.OrderByDescending(x => x.TransactionDateTime).FirstOrDefault()?.TransactionDateTime;
            if (_lastAsOf != null && currentAsof <= _lastAsOf)
                return _currentPortfolio;

            //sonst muss ich es neu rechnen
            _lastAsOf = currentAsof;

            return GetCurrentPortfolio(items);
        }

        private List<ITransaction> _transactions;
        private Dictionary<DateTime, List<ITransaction>> _dateTimeDictionary;

        public IEnumerable<ITransaction> GetCurrentHoldings(DateTime asof)
        {
            var items = _transactions ?? (_transactions = _cacheProvider.TransactionsCache.Value.Values.SelectMany(t => t).ToList());
            return GetCurrentPortfolio(items.Where(x => x.TransactionDateTime <= asof));
        }

        public IEnumerable<ITransaction> GetTransactions(DateTime asof)
        {
            //get datetime dic
            var dic = _dateTimeDictionary ?? (_dateTimeDictionary = _transactions.ToDictionaryList(x => x.TransactionDateTime));

            if (dic == null)
                return null;
            //ich returne am tading tag den portfoliostand vor der umschichtung + die umschichtungen separat, damit
            //die anzeige in der Gui klarer ist und nachvollzogen werden kann, was zu dem Stichtag geschehen ist
            return !dic.TryGetValue(asof, out var tradingDayTransactions)
                ? null
                : tradingDayTransactions;
        }

        /// <summary>
        /// Gibt nur einen Wert zurück wenn die Position im CurrentPrtfolio enthalten ist
        /// </summary>
        /// <param name="securityId"></param>
        /// <returns></returns>
        public int? GetCurrentShares(int securityId)
        {
            return CurrentPortfolio[securityId]?.Shares;
        }

        public IPortfolio GetCurrentPortfolio()
        {
            //Sollte nur beim Testen der Fall sein
            if (_cacheProvider == null)
                return _currentPortfolio ?? new CurrentPortfolio();

            return GetCurrentPortfolio(_cacheProvider.TransactionsCache.Value?.Values);
        }

        private IPortfolio GetCurrentPortfolio(IEnumerable<ITransaction> transactionItems)
        {
            if (transactionItems == null)
                return null;

            //temporäres dictionary erstellen
            var dic = new Dictionary<int, ITransaction>();

            //gruppuieren nach SecID => wenn der Count genau 1 ist kann ich sie einfach adden sonst muss ich summieren
            foreach (var secIdGrp in transactionItems.GroupBy(x => x.SecurityId))
            {
                if (secIdGrp.Count() == 1)
                    dic.Add(secIdGrp.Key, secIdGrp.First());
                else
                {
                    //wenn aktuellste Eintrag ein Close ist, kann ich die Transaktion ignorieren
                    if (secIdGrp.OrderByDescending(x => x.TransactionDateTime).FirstOrDefault()?.TransactionType == TransactionType.Close)
                        continue;

                    //darf nur beim letzen Open beginnen zu summieren!!
                    var transactions = secIdGrp.ToList();

                    //dazu gehe cih die Transaktionen vom aktuellesten beginnend durch
                    for (int i = transactions.Count - 1; i >= 0; i--)
                    {
                        var currentTransaction = transactions[i];
                        //sobald ich auf das letzte opening gestoßen bin remove ich alle alten aus der Liste und summiere nur die verbleibenden items
                        if (currentTransaction.TransactionType == TransactionType.Open)
                        {
                            transactions.RemoveRange(0, i);
                            break;
                        }
                    }

                    //sumItem erstellen
                    var sumItem = new Transaction();

                    // nur nicht gecancellte Transaktionen berücksichtigen
                    foreach (var item in transactions.Where(x => x.Cancelled != 1))
                    {
                        sumItem.SecurityId = secIdGrp.Key;
                        sumItem.Shares += item.Shares;
                        sumItem.TargetAmountEur += item.TargetAmountEur;
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

            return (_currentPortfolio = new CurrentPortfolio(dic.Values, _lastAsOf));
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

