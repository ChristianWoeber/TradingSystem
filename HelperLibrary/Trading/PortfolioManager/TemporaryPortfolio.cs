using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Trading.PortfolioManager.Settings;
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

        //#region TryHasCash


        //public bool TryHasCash(out decimal remainingCash)
        //{
        //    return _cashManager.TryHasCash(out remainingCash);
        //}

        //#endregion

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

        /// <summary>
        /// die Aktuelle Auslastung
        /// </summary>
        public decimal CurrentSumInvestedTargetWeight
        {
            get
            {
                return _items.Sum(t => t.EffectiveWeight);
            }
        }

       
        public void CancelCandidate(ITradingCandidate candidate)
        {
            if (candidate.IsInvested && !candidate.IsTemporary)
                throw new ArgumentException("Achtung das darf eigentlich nicht sein!");

            var transaction = Get(candidate.Record.SecurityId);
            if (transaction == null)
                throw new ArgumentException($"Achtung der Kandidate konnte im temporären Portfolio nicht gefunden werden {candidate}");
            transaction.Cancelled = 1;
        }

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

            item.CancelledEvent += OnTransactionCancelled;
            //hinzufügen
            _items.Add(item);
        }

        private void OnTransactionCancelled(object sender, EventArgs e)
        {
            if (!(sender is ITransaction transaction))
                return;

            if (transaction.Cancelled == 1)
                UpdateCash(transaction, true);
            else
            {
                //Sollte hier eigentlich im Backtest niemals hinkommen
                UpdateCash(transaction);
            }
        }

        private void UpdateCash(ITransaction item, bool reverse = false)
        {
            if (reverse)
            {
                //wenn das flag gestzt ist erhöhe ich das Cash bei einem kauf, weil ich die temporäre transakion storniere
                item.Shares *= -1;
            }

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
            var item = _items.Where(x => x.SecurityId == candidateSecurityId).OrderByDescending(x => x.TransactionDateTime).FirstOrDefault();
            if (item == null)
                throw new NullReferenceException($"mit der {candidateSecurityId} konnte kein temporäres item gefunden werden");
            return item;
        }

        public bool ContainsCandidate(ITradingCandidate temporaryCandidate, bool exact = true)
        {
            return exact
                ? _uniqueTransactions.TryGetValue(UniqueKeyProvider.CreateUniqueKey(temporaryCandidate), out var _)
                : _items.FirstOrDefault(x => x.SecurityId == temporaryCandidate.Record.SecurityId) != null;
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