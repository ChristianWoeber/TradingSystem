using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing
{
    public interface IRebalanceContext
    {
        decimal Delta { get; }

        IPortfolioSettings Settings { get; }

        decimal MinimumBoundary { get; }

        decimal MaximumBoundary { get; }
    }
}