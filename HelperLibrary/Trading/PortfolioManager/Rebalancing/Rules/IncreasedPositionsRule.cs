using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing.Rules
{
    /// <summary>
    /// Stellt sicher, dass aufgestockte Positionen mehr "Bedeutung" bekommen
    /// </summary>
    public class IncreasedPositionsRule : IRebalanceRule
    {
        public void Apply(ITradingCandidate candidate)
        {
            if (candidate.CurrentWeight > new decimal(0.1))
                candidate.RebalanceScore.Update(Context.Delta);
            if (candidate.CurrentWeight > new decimal(0.2))
                candidate.RebalanceScore.Update(Context.Delta * 2);
            if (candidate.CurrentWeight > new decimal(0.3))
                candidate.RebalanceScore.Update(Context.Delta * 3);
        }

        public IRebalanceContext Context { get; set; }
    }
}