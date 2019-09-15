namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Das Interface stellt die Property zur verfügung die angibt on ein Kandidat aufgestockt werden darf
    /// </summary>
    public interface IPositionIncrementationStrategy
    {
        /// <summary>
        /// gibt an ob ein Kandidate aufgestock werden darf
        /// </summary>
        bool IsAllowedToBeIncremented();
    }
}