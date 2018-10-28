using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Database.Models;
using HelperLibrary.Interfaces;
using Trading.DataStructures.Interfaces;
using Trading.DataStructures.Utils;

namespace HelperLibrary.Trading.PortfolioManager
{
    public class TemporaryPortfolio : ITemporaryPortfolio
    {
        #region Private Members

        private readonly Dictionary<string, ITransaction> _uniqueTransactions = new Dictionary<string, ITransaction>();
        private readonly List<ITransaction> _items = new List<ITransaction>();
        private readonly ISaveProvider _saveProvider;
        private readonly IAdjustmentProvider _adjustmentProvider;


        #endregion

        #region Constructor

        public TemporaryPortfolio(IAdjustmentProvider adjustmentprovider, ISaveProvider saveProvider = null)
        {
            _saveProvider = saveProvider ?? new DefaultSaveProvider();
            _adjustmentProvider = adjustmentprovider;
            _cashManager = adjustmentprovider.CashHandler;
        }

        private ICashManager _cashManager { get; }

        #endregion

        #region TryHasCash


        public bool TryHasCash(out decimal remainingCash)
        {
            return _cashManager.TryHasCash(out remainingCash);
        }

        #endregion

        #region Count

        /// <summary>
        /// Der Count
        /// </summary>
        public int Count => _items.Count;


        #endregion

        #region HasChanges

        /// <summary>
        /// Das Flag das angbit ob es zu Änderungen gekommen ist
        /// </summary>
        public bool HasChanges { get; set; }


        #endregion

        #region Save

        public bool IsTemporary(int secId)
        {
            var transaction = _items.OrderByDescending(x => x.TransactionDateTime).FirstOrDefault(x => x.SecurityId == secId);
            return transaction != null && transaction.IsTemporary;
        }

        public void SaveTransactions(ISaveProvider provider = null)
        {
            // wenn ich einen Save Provider bekomme beutze ich den
            if (provider != null)
                provider.Save(this);
            else
            {
                //sonst nehm ich den default provider
                if (_saveProvider == null)
                    throw new MissingMemberException("Achtung es wurde kein SaveProvider übergeben!!");

                _saveProvider.Save(this);
            }
        }



        #endregion

        #region Add

        public void Add(ITransaction item, bool isTemporary = true)
        {
            // das temporary flag setzen
            item.IsTemporary = isTemporary;

            //Has Changes True setzen
            HasChanges = isTemporary;

            //wenn es sich um einen geplanten Verkauf handelt, den erlös cashwirksam buchen
            if (item.IsTemporary)
            {
                UpdateCash(item);
            }
            try
            {
                _uniqueTransactions.Add(item.UniqueKey, item);
            }
            catch (Exception ex)
            {
                throw ex;

            }


            //hinzufügen
            _items.Add(item);
        }

        private void UpdateCash(ITransaction item)
        {
            if (item.Shares < 0)
                IncrementCash(item);
            else
                DecrementCash(item);
        }

        public void DecrementCash(ITransaction item)
        {
            _cashManager.Cash -= Math.Abs(item.EffectiveAmountEur);
        }

        public void IncrementCash(ITransaction item)
        {
            _cashManager.Cash += Math.Abs(item.EffectiveAmountEur);
        }

        public void AddRange(IEnumerable<ITransaction> items, bool isTemporary = true)
        {
            foreach (var item in items)
                Add(item, isTemporary);
        }


        #endregion

        #region Clear

        public void Clear()
        {
            //Has Changes false setzen
            HasChanges = false;
            _uniqueTransactions.Clear();
            _items.Clear();
        }

        public ITransaction Get(int candidateSecurityId)
        {
            var item = _items.FirstOrDefault(x => x.SecurityId == candidateSecurityId && x.IsTemporary);
            if (item == null)
                throw new NullReferenceException($"mit der {candidateSecurityId} konnte kein temporäres item gefunden werden");
            return item;
        }

        public bool ContainsCandidate(ITradingCandidate temporaryCandidate)
        {
            return _uniqueTransactions.TryGetValue(UniqueKeyProvider.CreateUniqueKey(temporaryCandidate), out var _);
        }

        #endregion



        #region Enumerator

        public IEnumerator<ITransaction> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        #endregion

    }
}