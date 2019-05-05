using HelperLibrary.Trading.PortfolioManager.Settings;
using Trading.DataStructures.Enums;
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
            //die Spannweite dritteln
            var span = (Context.Settings.MaximumPositionSize - Context.Settings.MinimumPositionSizePercent) / 3;

            if (candidate.StopLossMeta == null)
                return;
            //wenn die Position noch nicht länger als 20 Tage im Portfolio ist, return ich hier
            if (candidate.StopLossMeta.Opening.Asof.AddDays(20) <= candidate.PortfolioAsof)
                return;

            //wenn die Position am High ist und beretis einaml aufgestock wurde deutlich erhhöhen
            if (candidate.StopLossMeta.High.Asof == candidate.Record.Asof && candidate.CurrentWeight > span)
                candidate.RebalanceScore.Update(Context.Delta * new decimal(1.5));

            //wenn die Position am High ist und größer als 3 * der span ist (=maximum) deutlich erhhöhen
            if (candidate.StopLossMeta.High.Asof == candidate.Record.Asof && candidate.CurrentWeight > span * 3)
                candidate.RebalanceScore.Update(Context.Delta * 3);

            //wenn der aktuelle Preis < als das opening ist, reduzieren
            if (candidate.Record.AdjustedPrice < candidate.StopLossMeta.Opening.AdjustedPrice)
                candidate.RebalanceScore.Update(Context.Delta, false);
        }

        public IRebalanceContext Context { get; set; }
    }
}