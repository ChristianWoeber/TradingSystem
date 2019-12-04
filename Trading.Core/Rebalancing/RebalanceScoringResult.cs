using Newtonsoft.Json;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Rebalancing
{
    public class RebalanceScoringResult : IRebalanceScoringResult
    {
        [JsonProperty]
        private readonly IScoringResult _performanceScoringResult;

        public RebalanceScoringResult()
        {

        }

        public RebalanceScoringResult(IScoringResult score, ITradingCandidate tradingCandidate, IPortfolioSettings settings)
        {
            _performanceScoringResult = score;

            //wenn der Kandidat nahe des Maximums ist und noch eine Positive Performance aufweisst aber sein Performance Score eigenltich negativ ist, weil die jüngste vergangenheit negativ ist kann ich über das flag steuern ob statt des Performance scores dier Performance genommen werden soll
            if (tradingCandidate.CurrentWeight >= settings.MaximumPositionSize - settings.MaximumPositionSizeBuffer && tradingCandidate.Performance > 0)
            {
                if ((_performanceScoringResult.Score < 0 /*|| _performanceScoringResult.Score / 100 < tradingCandidate.Performance*/)
                    && settings.UseAbsoluteValueForRebalanceScoringResult)
                {
                    Score = tradingCandidate.Performance * 100;
                }
            }
            else
            {
                Score = _performanceScoringResult.Score;
            }

        }

        /// <summary>
        /// Der Score des Rebalancing Ergebnisses
        /// </summary>
        public decimal Score { get; set; }

        /// <summary>
        /// Methode zum Updaten des Scores
        /// </summary>
        /// <param name="updateValue"></param>
        /// <param name="increment"></param>
        public void Update(decimal updateValue, bool increment = true)
        {
            var update = increment
                ? (updateValue * _performanceScoringResult.Score)
                : (-updateValue * _performanceScoringResult.Score);
            Score += update;
        }
    }
}