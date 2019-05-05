using System;
using HelperLibrary.Trading.PortfolioManager.Rebalancing;
using HelperLibrary.Trading.PortfolioManager.Settings;
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
        private readonly ITransactionsHandler _transactionsHandler;
        [JsonProperty]
        [JsonConverter(typeof(PortfolioValuationConverter))]
        private readonly IAdjustmentProvider _adjustmentProvider;

        public TradingCandidate()
        {

        }

        public TradingCandidate(ITradingCandidateBase tradingCandidateBase, ITransactionsHandler transactionsHandler, IAdjustmentProvider adjustmentProvider, bool isInvested = false)
        {
            _tradingCandidateBase = tradingCandidateBase;
            _transactionsHandler = transactionsHandler;
            _adjustmentProvider = adjustmentProvider;

            //Initialisierungen
            IsInvested = isInvested;
            PortfolioAsof = adjustmentProvider.PortfolioAsof;
            RebalanceScore = new RebalanceScoringResult(tradingCandidateBase.ScoringResult);

            Record = tradingCandidateBase.Record;
            ScoringResult = tradingCandidateBase.ScoringResult;
            AveragePrice = transactionsHandler.GetAveragePrice(SecurityId, PortfolioAsof) ?? Record.AdjustedPrice;
            LastTransaction = transactionsHandler.GetSingle(SecurityId, null);
            CurrentWeight = GetCurrentWeight();

            //Das Target Weight wird auch mit dem current initialisiert
            TargetWeight = CurrentWeight;
            if (IsInvested)
                CurrentPosition = transactionsHandler.CurrentPortfolio[SecurityId];

        }

        /// <summary>
        /// Berechnet das Aktuelle Gewicht auf Basis des aktuellen Preises
        /// </summary>
        /// <returns></returns>
        private decimal GetCurrentWeight()
        {
            var shares = _transactionsHandler.GetCurrentShares(SecurityId);
            if (shares == null)
                return 0;

            return (Record.AdjustedPrice * shares.Value) / _adjustmentProvider.PortfolioValue;
        }

        /// <summary>
        /// Die aktulle Position im Portfolio
        /// </summary>
        public ITransaction CurrentPosition { get; set; }

        /// <summary>
        /// Die Security Id des Candidaten
        /// </summary>
        public int SecurityId => _tradingCandidateBase.Record?.SecurityId ?? 0;

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

        ////gibt an ob der aktuelle Score höher ist als der letzte
        //// muss mindestens 25% besser sein und es darf aktuell kein Stop ausgelöst worden sein
        ////TODO: PositionWatcher hier verwenden!
        public bool HasBetterScoring
        {
            get
            {
                var meta = _adjustmentProvider.PositionWatcher.GetStopLossMeta(this);
                if (meta.High.Asof == Record.Asof)
                    return true;
                return false;
                //return ScoringResult.Score * new decimal(1.25) > LastScoringResult?.Score && !IsBelowStopp;
            }
        }

        public IStopLossMeta StopLossMeta => _adjustmentProvider.PositionWatcher.GetStopLossMeta(this);

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

        /// <summary>
        /// Der Score wonach die Kandidaten im RebelanceProvider sortiert werden
        /// Die Basis ist der aktuelle Score
        /// </summary>
        public IRebalanceScoringResult RebalanceScore { get; set; }

        /// <summary>
        /// Wenn der TransaktionsType Changed ist und das Zielgewicht kleiner als das aktuelle dann ist bereits eine Abschichtung erfolgt
        /// </summary>
        public bool IsTemporarySell => TransactionType == TransactionType.Changed && TargetWeight < CurrentWeight;

        public override string ToString()
        {
            return
                $"{Name} | Score: {Score} | | {nameof(RebalanceScore)}: {RebalanceScore.Score} | Invested: {IsInvested} | IsTemporary: {IsTemporary} | CurrentWeight: {CurrentWeight:N} | TargetWeight: {TargetWeight:N} " +
                $"| CurrentPrice: {Record.AdjustedPrice:N} | AveragePrice: {AveragePrice:N}  | TransactionType: {TransactionType} " +
                $"| SecurityId: {Record.SecurityId} ";
            ;
        }
    }
}
