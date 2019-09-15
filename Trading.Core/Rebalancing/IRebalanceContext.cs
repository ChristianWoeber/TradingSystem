using Trading.DataStructures.Interfaces;

namespace Trading.Core.Rebalancing
{
    public interface IRebalanceContext
    {
        decimal Delta { get; }

        IPortfolioSettings Settings { get; }

        decimal MinimumBoundary { get; }

        decimal MaximumBoundary { get; }
    }
}