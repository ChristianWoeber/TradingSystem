namespace Trading.DataStructures.Interfaces
{
    public interface IDailyReturnMetaInfo
    {
        /// <summary>
        /// Der komplette Record from
        /// </summary>
        ITradingRecord FromRecord { get; }

        /// <summary>
        /// Der komplette Record to
        /// </summary>
        ITradingRecord ToRecord { get; }

        /// <summary>
        /// der Return 
        /// </summary>
        decimal AbsoluteReturn { get; }
    }
}