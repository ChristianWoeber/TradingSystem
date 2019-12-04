using System;
using Newtonsoft.Json;
using Trading.Core.Converter;
using Trading.Core.Rebalancing;
using Trading.Core.Strategies;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Candidates
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

        private readonly IPortfolioSettings _settings;

        public TradingCandidate()
        {

        }

        public TradingCandidate(ITradingCandidateBase tradingCandidateBase, ITransactionsHandler transactionsHandler, IAdjustmentProvider adjustmentProvider, IPortfolioSettings settings, bool isInvested = false)
        {
            _tradingCandidateBase = tradingCandidateBase;
            _transactionsHandler = transactionsHandler;
            _adjustmentProvider = adjustmentProvider;
            _settings = settings;

            //Initialisierungen
            IsInvested = isInvested;
            PortfolioAsof = adjustmentProvider.PortfolioAsof;
            IncrementationStrategyProvider = new DefaultIncrementationStrategy(this, _adjustmentProvider, settings);

            Record = tradingCandidateBase.Record;
            ScoringResult = tradingCandidateBase.ScoringResult;
            AveragePrice = transactionsHandler.GetAveragePrice(SecurityId, PortfolioAsof) ?? Record.AdjustedPrice;
            LastTransaction = transactionsHandler.GetSingle(SecurityId, null);
            CurrentWeight = GetCurrentWeight();
            RebalanceScore = new RebalanceScoringResult(tradingCandidateBase.ScoringResult,this,settings);

            //Das Target Weight wird auch mit dem current initialisiert
            TargetWeight = CurrentWeight;
            if (IsInvested)
                CurrentPosition = transactionsHandler.CurrentPortfolio[SecurityId];

            PerformanceUnderlying = _adjustmentProvider.PositionWatcher.GetUnderlyingPerformance(SecurityId);
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

            return Math.Round((Record.AdjustedPrice * shares.Value) / _adjustmentProvider.PortfolioValue, 4);
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

        /// <summary>
        /// Die Totale Performance des UNderlyings seit Eröffnung
        /// </summary>
        public decimal? PerformanceUnderlying { get; }


        ///// <summary>
        ///// Gibt on ob ich die aktuelle Position erhöhen darf
        ///// </summary>
        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //public bool CanBeIncremented
        //{
        //    get
        //    {
        //        //gibt an ob sich die Position unter den Top 5 Performern, gemessen am Total Return des Position, befindet
        //        //Der Top errechnet sich nun dynamisch aus der maximalen Positionsgröße
        //        var isUnderTopPositions = _adjustmentProvider.PositionWatcher.IsUnderTopPositions(SecurityId, (int)(1 / _settings.MaximumPositionSize));
        //        var meta = _adjustmentProvider.PositionWatcher.GetStopLossMeta(this);
        //        if (Record.AdjustedPrice >= meta?.High.Price && isUnderTopPositions)
        //            return true;
        //        return false;
        //    }
        //}

        /// <summary>
        /// Gibt die Info zur StopLoss Meta nach aussen
        /// </summary>
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
        /// Das Interface das die Property bereitsstellt die angibt ob ein Kandiate zum jeweiligen Zeitpunkt aufgestockt werden darf vom Handelssystem
        /// </summary>
        public IPositionIncrementationStrategy IncrementationStrategyProvider { get; }

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
