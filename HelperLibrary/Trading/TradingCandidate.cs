using System;
using HelperLibrary.Database.Interfaces;
using HelperLibrary.Database.Models;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;

namespace HelperLibrary.Trading
{
    public interface ITradingCandidateBase
    {
        ITradingRecord Record { get; }

        IScoringResult ScoringResult { get; }
    }

    public class TradingCandidate
    {
        private readonly ITradingCandidateBase _tradingCandidateBase;

        internal int SecurityId => _tradingCandidateBase.Record.SecurityId;

        public TradingCandidate(ITradingCandidateBase tradingCandidateBase, ITransactionsHandler transactionsHandler, DateTime asof, bool isInvested = false)
        {
            _tradingCandidateBase = tradingCandidateBase;

            //Initialisierungen
            IsInvested = isInvested;
            Record = _tradingCandidateBase.Record;
            ScoringResult = _tradingCandidateBase.ScoringResult;
            AveragePrice = transactionsHandler.GetAveragePrice(SecurityId, asof) ?? Record.AdjustedPrice;
            LastTransaction = transactionsHandler.GetSingle(SecurityId, null);
            CurrentWeight = transactionsHandler.GetWeight(SecurityId) ?? 0;
            //Das Target Weight wird auch mit dem current initialisiert
            TargetWeight = CurrentWeight;
        }
        /// <summary>
        /// die letzte Transaktion
        /// </summary>
        public TransactionItem LastTransaction { get; set; }

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

        public override string ToString()
        {
            return $"{Name} | Score: {Score} | Invested: {IsInvested} | IsTemporary: {IsTemporary} | CurrentWeight: {CurrentWeight:N} | TargetWeight: {TargetWeight:N} | CurrentPrice: {Record.AdjustedPrice:N} | AveragePrice: {AveragePrice:N} | HasBetterScoring: {HasBetterScoring}";
        }
    }


}
