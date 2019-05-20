using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing.Rules
{
    /// <summary>
    /// Reduziert den Wert für unknown und opening deutlich
    /// </summary>
    public class TransactionTypRule : IRebalanceRule
    {
        public void Apply(ITradingCandidate candidate)
        {
            switch (candidate.TransactionType)
            {
                case TransactionType.Open:
                    candidate.RebalanceScore.Update(Context.Delta * 5, false);
                    break;
                case TransactionType.Unknown:
                    candidate.RebalanceScore.Update(Context.Delta * 8, false);
                    break;
            }
        }

        public IRebalanceContext Context { get; set; }
    }
}