using System;
using Trading.Core.Scoring;

namespace Trading.UI.Wpf
{
    public class NewHighsCountScoringResult : ScoringResult
    {
        public NewHighsCountScoringResult() : base()
        {

        }

        /// <summary>der Score der sich aus den Berechnungen ergibt</summary>
        public override decimal Score
        {
            get
            {
                if (LowMetaInfo == null)
                    return 0;
                //Wenn die vola größer oder gleich 1 ist, dann handelt es sich um keinen validen kandidaten
                if (Volatility >= 1)
                    return 0;

                // Ich gewichte die Performance,
                // die aktuellsten Daten haben die größten Gewichte
                //ich ziehe auch noch den maxdrawdown in der Periode ab
                var avgPerf = Performance10 * (decimal)0.10
                              + Performance30 * (decimal)0.40
                              + Performance90 * (decimal)0.40
                              + Performance250 * (decimal)0.10;

                var increaseFactor = 1 + LowMetaInfo.NewHighsCollection.Count * 0.05M;
                var newHighsAdjusted = avgPerf * increaseFactor;

                var increaseFactorPositve = 1 + LowMetaInfo.PositiveDailyRetunsMetaInfo.Count * 0.01M;
                var newHighsAndPositiveAdjusted = newHighsAdjusted * increaseFactorPositve;

                //danach zinse ich quais die vola ab wenn null dann nehm ich als default 35%
                return Math.Round(newHighsAndPositiveAdjusted * (1 - Volatility ?? 0.35M) * 100, 2);
            }
        }
    }
}