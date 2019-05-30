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
            if (!candidate.IsInvested)
                return;

            //if (candidate.Performance <= 0)
            //    return;

            //Will hier nur investierte Positionen mit positiver Performance stärken
            var span = (Context.Settings.MaximumPositionSize - Context.Settings.MaximumInitialPositionSize) / 3 - 0.01M;

            if (candidate.CurrentWeight >= span)
                candidate.RebalanceScore.Update(Context.Delta * 3);
            if (candidate.CurrentWeight > span * 2)
                candidate.RebalanceScore.Update(Context.Delta * 4);
            if (candidate.CurrentWeight > span * 3)
                candidate.RebalanceScore.Update(Context.Delta * 5);
        }

        public IRebalanceContext Context { get; set; }
    }

    /// <summary>
    /// Erhöht den Score für investierte Positionen die im plus sind für jeden Prozent um 10%
    /// </summary>
    public class IncreaseStrongPerformancePositionsRule : IRebalanceRule
    {
        public void Apply(ITradingCandidate candidate)
        {
            if (!candidate.IsInvested)
                return;

            if ( candidate.PerformanceUnderlying <= 0 || candidate.PerformanceUnderlying == null)
                return;
            var update = 1M + candidate.PerformanceUnderlying.Value * 10M;
            candidate.RebalanceScore.Update(update);
        }

        public IRebalanceContext Context { get; set; }
    }

}