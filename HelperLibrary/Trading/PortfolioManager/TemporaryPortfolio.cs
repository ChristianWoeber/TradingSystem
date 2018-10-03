using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using HelperLibrary.Database.Models;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    public class TemporaryPortfolio : ITemporaryPortfolio
    {
        #region Private Members


        private readonly List<Transaction> _items = new List<Transaction>();
        private readonly ISaveProvider _saveProvider;
        private readonly IAdjustmentProvider _adjustmentProvider;


        #endregion

        #region Constructor

        public TemporaryPortfolio(IPortfolioSettings settings, IAdjustmentProvider adjustmentprovider, ISaveProvider saveProvider = null)
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

        public void Add(Transaction item, bool isTemporary = true)
        {
            // das temporary flag setzen
            item.IsTemporary = isTemporary;

            //Has Changes True setzen
            HasChanges = isTemporary;

            //wenn es sich um einen geplanten Verkauf handelt, den erlös cashwirksam buchen
            if (item.IsTemporary)
            {
                if (item.Shares < 0)
                {
                    _cashManager.Cash += Math.Abs(item.EffectiveAmountEur);
                    //if (Debugger.IsAttached)
                    //    Trace.TraceInformation("Cash erhöht um " + Math.Abs(item.EffectiveAmountEur));

                }
                else
                {
                    _cashManager.Cash -= Math.Abs(item.EffectiveAmountEur);
                    //if (Debugger.IsAttached)
                    //    Trace.TraceInformation("Cash verringert um " + Math.Abs(item.EffectiveAmountEur));
                }
            }

            //hinzufügen
            _items.Add(item);
        }

        public void AddRange(IEnumerable<Transaction> items, bool isTemporary = true)
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

            _items.Clear();
        }

        public Transaction Get(int candidateSecurityId)
        {
            var item = _items.FirstOrDefault(x => x.SecurityId == candidateSecurityId && x.IsTemporary);
            if (item == null)
                throw new NullReferenceException($"mit der {candidateSecurityId} konnte kein temporäöres item gefunden werden");
            return item;
        }

        #endregion

        #region RebuildTemporaryPortfolio

        //TODO: Rebuild implementieren
        //public void RebuildPortfolio(IScoringProvider scoreProvider, DateTime asof)
        //{
        //    //Wenn alle Items als temporär geflaggt sind, handelt es sich um bestehnde neue investments, dann brauch ich nicht abschichten, weil die besten Kandiaten schon im portfolio sind 
        //    if (_items.TrueForAll(x => x.IsTemporary))
        //        return;

        //    //bzw, wenn noch Cash verfügbar ist brauch ich nichts unternehemen
        //    if (TryHasCash(out var remainingCash))
        //        return;

        //    // Wenn es keien Änderungen gibt
        //    if (!HasChanges)
        //        return;

        //    //Wenn kein Item als temporär gekennzeichnet ist, dann hat sich im Portfolio nichts geändert und ich breche an dieser Stelle ab
        //    if (_items.TrueForAll(x => !x.IsTemporary))
        //        return;

        //    //sonst muss ich zusätzlches Cash schaffen, indem ich den schlechtesten Kandiaten verkaufe
        //    var notTemporarylist = new List<ITradingCandidateBase>();
        //    var temporaryList = new List<ITradingCandidateBase>();

        //    //alle nicht temporären = bestehenden Investments stehen zum Abschichten zur Verfügung
        //    foreach (var currentInvestment in _items.Where(x => x.Shares > 0))
        //    {
        //        var score = scoreProvider.GetScore(currentInvestment.SecurityId, asof);
        //        var record = scoreProvider.GetTradingRecord(currentInvestment.SecurityId, asof);
        //        if (!currentInvestment.IsTemporary)
        //            notTemporarylist.Add(new Candidate(record, score));
        //        else
        //            temporaryList.Add(new Candidate(record, score));
        //    }

        //    while (true)
        //    {
        //        //TODO: MinimumHolding Period berücksichtigen
        //        //nach schlechtesten aufsteigen sortieren
        //        foreach (var candidate in notTemporarylist.OrderBy(x => x.ScoringResult.Score))
        //        {
        //            //wenn auch nach dem Verkauf noch kein Cash zur Verfügnug steht
        //            //schichte ich weiter ab, TryHasCash gibt erst true zurück, wenn sich eine neue Position ausgeht
        //            //hier reicht mit aber, dass ich Deckung am CashAccount hab (dann gehen sich alle temporären Transaktionen
        //            //aus und ich kann breaken
        //            if (!TryHasCash(out var remainingNewCash))
        //            {
        //                //die Position mit dem schlechtesten Score abschichten, bzw. TotalVerkaufen
        //                //wenn true zurückgegeben wird, dannn reicht die eine position aus und ich kann breaken, sonst schichte ich weiter ab
        //                if (_adjustmentProvider.AdjustTemporaryPortfolioToCashPuffer(remainingNewCash, TransactionType.Changed, candidate))
        //                    break;
        //                continue;
        //            }

        //            if (remainingNewCash > 0)
        //                break;

        //            //alle nicht temporären = bestehenden Investments stehen zum Abschichten zur Verfügung
        //            //sonst muss ich die temporären nach score kicken
        //            //sonst muss ich zusätzlches Cash schaffen, indem ich den schlechtesten temporären Kandiaten verkaufe
        //            foreach (var temporaryCandidate in temporaryList.OrderBy(x => x.ScoringResult.Score))
        //            {

        //                if (_adjustmentProvider.AdjustTemporaryPortfolioToCashPuffer(remainingNewCash, TransactionType.Changed, temporaryCandidate))
        //                    break;
        //            }

        //        }

        //        //breaken aus der while
        //        break;
        //    }
        //}


        #endregion

        #region Enumerator

        public IEnumerator<Transaction> GetEnumerator()
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