using System;
using HelperLibrary.Util.Atrributes;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Database.Models
{
    /// <summary>
    /// das Model zum Speichern des Rebalance Scores, durch die Transaktion kann auch wieder ein mapping dazu hergestellt werden
    /// </summary>
    public class ScoringTraceModel : IInputMappable
    {
        /// <summary>
        /// Für den Activator
        /// </summary>
        public ScoringTraceModel()
        {

        }

        public ScoringTraceModel(ITransaction transaction, IScoringResult scoringResult, IRebalanceScoringResult rebalanceScore, DateTime portfolioAsof)
        {
            TransactionDateTime = transaction.TransactionDateTime;
            PortfolioAsof = portfolioAsof;
            SecurityId = transaction.SecurityId;
            RebalanceScore = rebalanceScore.Score;
            PerformanceScore = scoringResult.Score;
            PortfolioAsof = portfolioAsof;
            Shares = transaction.Shares;
            TransactionType = (int)transaction.TransactionType;
        }

        public ScoringTraceModel(ITradingCandidate candidate, DateTime portfolioAsof)
        {
            PerformanceScore = candidate.ScoringResult.Score;
            RebalanceScore = candidate.RebalanceScore.Score;
            PortfolioAsof = portfolioAsof;
            SecurityId = candidate.Record.SecurityId;
           
        }

        [InputMapping(KeyWords = new[] { nameof(TransactionDateTime) })]
        public DateTime? TransactionDateTime { get; set; }

        [InputMapping(KeyWords = new[] { nameof(PortfolioAsof) })]
        public DateTime PortfolioAsof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Shares) })]
        public decimal? Shares { get; set; }

        [InputMapping(KeyWords = new[] { nameof(TransactionType) })]
        public int? TransactionType { get; set; }

        [InputMapping(KeyWords = new[] { nameof(SecurityId) })]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new[] { nameof(PerformanceScore) })]
        public decimal PerformanceScore { get; set; }

        [InputMapping(KeyWords = new[] { nameof(RebalanceScore) })]
        public decimal RebalanceScore { get; set; }

        /// <summary>
        /// Der Mapping Key für das spätere zuordnen zu einer Transaktion
        /// </summary>
        public string TransactionMappingKey => $"{TransactionDateTime:d}_{PortfolioAsof:d}_{Shares}_{TransactionType}_{SecurityId}";

        /// <summary>
        /// Der Mapping Key für das spätere zuordnen zu einer Transaktion
        /// </summary>
        public string PortfolioAsofMappingKey => $"{PortfolioAsof:d}_{SecurityId}";


    }
}