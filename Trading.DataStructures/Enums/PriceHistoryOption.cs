namespace Trading.DataStructures.Enums
{
    public enum PriceHistoryOption
    {
        /// <summary>
        /// Gibt wenn zu dem Stichtag kein Item gefunden wurde das Vorherige zurück
        /// </summary>
        PreviousItem,
        /// <summary>
        /// Gibt wenn zu dem Stichtag kein Item gefunden wurde das Nächste zurück
        /// </summary>
        NextItem,
        /// <summary>
        /// Gibt immer das Item mit dem Preis vom Vortag zuzrück (nützlich für Backtests)
        /// </summary>
        PreviousDayPrice
    }
}