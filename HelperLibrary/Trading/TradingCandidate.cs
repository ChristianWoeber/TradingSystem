using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HelperLibrary.Database.Interfaces;
using HelperLibrary.Database.Models;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;
using JetBrains.Annotations;

namespace HelperLibrary.Trading
{
    public interface ITradingCandidateBase
    {
        ITradingRecord Record { get; }

        IScoringResult ScoringResult { get; }
    }

    public interface ILockupHandler
    {
        
    }

    public class TradingCandidate
    {
        private readonly ITradingCandidateBase _tradingCandidateBase;

        internal int SecurityId => _tradingCandidateBase.Record.SecurityId;

        public TradingCandidate(ITradingCandidateBase tradingCandidateBase, ITransactionsHandler transactionsHandler, PortfolioManager.PortfolioManager pm, bool isInvested = false)
        {
            _tradingCandidateBase = tradingCandidateBase;
           
            //Initialisierungen
            IsInvested = isInvested;
            PortfolioAsof = pm.PortfolioAsof;

            Record = _tradingCandidateBase.Record;
            ScoringResult = _tradingCandidateBase.ScoringResult;
            AveragePrice = transactionsHandler.GetAveragePrice(SecurityId, PortfolioAsof) ?? Record.AdjustedPrice;
            LastTransaction = transactionsHandler.GetSingle(SecurityId, null);
            CurrentWeight = transactionsHandler.GetWeight(SecurityId) ?? 0;

            //Das Target Weight wird auch mit dem current initialisiert
            TargetWeight = CurrentWeight;         
        }

        public ILockupHandler LockupHandler { get; set; }

        public DateTime PortfolioAsof { get; set; }

        /// <summary>
        /// die letzte Transaktion
        /// </summary>
        public Transaction LastTransaction { get; set; }

        /// <summary>
        /// der aktuelle Score
        /// </summary>
        public IScoringResult ScoringResult { get; }

        /// <summary>
        /// Der aktuelle Record
        /// </summary>
        public ITradingRecord Record { get; }

        /// <summary>
        /// das Metadaten flag das angibt ob in dem Candidaten gerade ein aktives investment besteht 
        /// </summary>
        public bool IsInvested { get; }

        /// <summary>
        /// das Metadaten flag das angibt ob der Candidate gerade im tempor#ren portfolio ist (also ein investment geplant ist) 
        /// </summary>
        public bool IsTemporary { get; set; }

        /// <summary>
        /// das Metadaten flag das angibt, ob der Candidate ausgestoppt wurde 
        /// </summary>
        public bool HasStopp { get; set; }

        /// <summary>
        /// das letzte Scoring Result 
        /// </summary>
        public IScoringResult LastScoringResult { get; set; }

        /// <summary>
        /// Das neue Zielgewicht
        /// </summary>
        public decimal TargetWeight { get; set; }

        /// <summary>
        /// das aktuelle gewicht im Portfolio
        /// </summary>
        public decimal CurrentWeight { get; set; }

        /// <summary>
        /// der durchschnittspreis des Candidaten
        /// </summary>
        public decimal AveragePrice { get; }

        /// <summary>
        /// die aktuelle Performance
        /// </summary>
        public decimal Performance => 1 - AveragePrice / Record.AdjustedPrice;

        //gibt an ob der aktuelle Score höher ist als der letzte
        public bool HasBetterScoring => ScoringResult.Score > LastScoringResult?.Score;

        /// <summary>
        /// Der Name des Records
        /// </summary>
        public string Name => Record.Name;

        /// <summary>
        /// Der Name des Records
        /// </summary>
        public decimal Score => ScoringResult.Score;

        /// <summary>
        /// der Typ der Transaktion
        /// </summary>
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// Gibt an ob die Position unter dem Limit ist (die exekution des stopss kann aber aufgrund von lockups noch nach hinten verschoben werden)
        /// </summary>
        public bool IsBelowStopp { get; set; }


        public override string ToString()
        {
            return $"{Name} | Score: {Score} | Invested: {IsInvested} | IsTemporary: {IsTemporary} | CurrentWeight: {CurrentWeight:N} | TargetWeight: {TargetWeight:N} | CurrentPrice: {Record.AdjustedPrice:N} | AveragePrice: {AveragePrice:N} | HasBetterScoring: {HasBetterScoring}";
        }
    }

    internal class LockupPeriodeHandler : ILockupHandler
    {
        private readonly TradingCandidate _tradingCandidate;
        private readonly IPortfolioSettings _pmPortfolioSettings;  

        public LockupPeriodeHandler(TradingCandidate tradingCandidate, IPortfolioSettings pmPortfolioSettings)
        {
            _tradingCandidate = tradingCandidate;
            _pmPortfolioSettings = pmPortfolioSettings;
           
        }

        public TransactionType Type
        {
            get => _tradingCandidate.TransactionType;
            set
            {
                if (value == _tradingCandidate.TransactionType)
                    return;
                _tradingCandidate.TransactionType = value;
                OnTransactionTypeChanged();
            }
        }

        private void OnTransactionTypeChanged()
        {
            switch (Type)
            {
                case TransactionType.Open:

                    break;
               
                case TransactionType.Changed:
                    break;
      
            }
        }

    }
}
