using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing.Rules
{
    /// <summary>
    /// Stellt sicher, dass die besseren Kandidaten mehr Gewicht erhalten
    /// </summary>
    public class HasBetterScoringRule : IRebalanceRule
    {
        public void Apply(ITradingCandidate candidate)
        {
            if (candidate.HasBetterScoring)
                candidate.RebalanceScore.Update(Context.Delta);
        }

        public IRebalanceContext Context { get; set; }
    }
}