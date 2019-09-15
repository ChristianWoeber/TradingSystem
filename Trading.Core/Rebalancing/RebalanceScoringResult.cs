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

        public RebalanceScoringResult(IScoringResult score)
        {
            _performanceScoringResult = score;
            Score = _performanceScoringResult.Score;
        }

        public decimal Score { get; set; }

        public void Update(decimal updateValue, bool increment = true)
        {
            var update = increment
                ? (updateValue * _performanceScoringResult.Score)
                : (-updateValue * _performanceScoringResult.Score);
            Score += update;
        }
    }
}