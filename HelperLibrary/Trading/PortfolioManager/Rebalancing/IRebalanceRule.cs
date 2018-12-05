using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing
{
    public interface IRebalanceRule
    {
        /// <summary>
        /// Führt die Regel aus und übergibt den TradingCandidate
        /// </summary>
        /// <param name="candidate"></param>
        void Apply(ITradingCandidate candidate);

        /// <summary>
        /// der Context der noch settings enthält
        /// </summary>
        IRebalanceContext Context { get; set; }
    }
}