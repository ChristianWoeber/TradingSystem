using System;
using Newtonsoft.Json;
using Trading.DataStructures.Interfaces;
using Trading.DataStructures.Enums;
using HelperLibrary.Util.Converter;

namespace HelperLibrary.Trading
{

    public class TradingCandidate : ITradingCandidate
    {
        [JsonProperty]
        private readonly ITradingCandidateBase _tradingCandidateBase;
        [JsonProperty]
        //[JsonConverter(typeof(TransactionsHandlerConverter))]
        private readonly ITransactionsHandler _transactionsHandler;
        [JsonProperty]
        [JsonConverter(typeof(PortfolioValuationConverter))]
        private readonly IPortfolioValuation _valuation;

        public TradingCandidate()
        {
           
        }

        public TradingCandidate(ITradingCandidateBase tradingCandidateBase, ITransactionsHandler transactionsHandler, IPortfolioValuation valuation, bool isInvested = false)
        {
            _tradingCandidateBase = tradingCandidateBase;
            _transactionsHandler = transactionsHandler;
            _valuation = valuation;

            //Initialisierungen
            IsInvested = isInvested;
            PortfolioAsof = valuation.PortfolioAsof;

            Record = tradingCandidateBase.Record;
            ScoringResult = tradingCandidateBase.ScoringResult;
            AveragePrice = transactionsHandler.GetAveragePrice(SecurityId, PortfolioAsof) ?? Record.AdjustedPrice;
            LastTransaction = transactionsHandler.GetSingle(SecurityId, null);
            CurrentWeight = transactionsHandler.GetWeight(SecurityId) ?? 0;

            //Das Target Weight wird auch mit dem current initialisiert
            TargetWeight = CurrentWeight;
            if (IsInvested)
                CurrentPosition = transactionsHandler.CurrentPortfolio[SecurityId];
        }
        /// <summary>
        /// Die aktulle Position im Portfolio
        /// </summary>
        public ITransaction CurrentPosition { get; set; }

        /// <summary>
        /// Die Security Id des Candidaten
        /// </summary>
        public int SecurityId => _tradingCandidateBase.Record.SecurityId;

        /// <summary>
        /// Das Portfolio-Datum
        /// </summary>
        public DateTime PortfolioAsof { get; set; }

        /// <summary>
        /// die letzte Transaktion
        /// </summary>
        public ITransaction LastTransaction { get; set; }

        /// <summary>
        /// der aktuelle Score
        /// </summary>
        public IScoringResult ScoringResult { get; set; }

        /// <summary>
        /// Der aktuelle Record
        /// </summary>
        public ITradingRecord Record { get; set; }

        /// <summary>
        /// das Metadaten flag das angibt ob in dem Candidaten gerade ein aktives investment besteht 
        /// </summary>
        public bool IsInvested { get; set; }

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
        public decimal AveragePrice { get; set; }

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
            return $"{Name} | Score: {Score} | Invested: {IsInvested} | IsTemporary: {IsTemporary} | CurrentWeight: {CurrentWeight:N} | TargetWeight: {TargetWeight:N} " +
                   $"| CurrentPrice: {Record.AdjustedPrice:N} | AveragePrice: {AveragePrice:N} | HasBetterScoring: {HasBetterScoring} | TransactionType: {TransactionType} " +
                   $"| SecurityId: {Record.SecurityId}";
        }
    }

}
