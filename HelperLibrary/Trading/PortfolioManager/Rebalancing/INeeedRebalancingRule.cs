using System.Collections.Generic;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing
{
    public interface INeeedRebalanceRule
    {
        /// <summary>
        /// Sobald in eine der Regeln true zurückgibt gehe ich ins rebalancing
        /// </summary>
        bool Apply(IEnumerable<ITradingCandidate> candidates);

        /// <summary>
        /// die reihenfolge nach der die Regeln sortiert werden können
        /// </summary>
        int SortIndex { get; set; }

        /// <summary>
        /// der Context der noch settings enthält
        /// </summary>
        IRebalanceContext Context { get; set; }
    }
}