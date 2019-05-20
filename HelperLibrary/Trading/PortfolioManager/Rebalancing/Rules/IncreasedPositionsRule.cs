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
                candidate.RebalanceScore.Update(Context.Delta *3);
            if (candidate.CurrentWeight > span * 2)
                candidate.RebalanceScore.Update(Context.Delta * 4);
            if (candidate.CurrentWeight > span * 3)
                candidate.RebalanceScore.Update(Context.Delta * 5);
        }

        public IRebalanceContext Context { get; set; }
    }
}