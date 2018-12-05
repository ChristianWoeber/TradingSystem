namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Das Interface für das Rebalance der Positionen
    /// </summary>
    public interface IRebalanceScoringResult
    {
        decimal Score { get; set; }

        void Update(decimal delta, bool increment = true);

    }
}