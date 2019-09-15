namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// das Interface erweitert den Basis IPriceRecord
    /// </summary>
    public interface ITradingRecord : IPriceRecord
    {
        /// <summary>
        /// die SecurityId
        /// </summary>
        int SecurityId { get; set; }

        /// <summary>
        /// Der Name des Wertpapiers
        /// </summary>
        string Name { get; set; }
    }
}
