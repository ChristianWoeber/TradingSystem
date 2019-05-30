namespace Trading.DataStructures.Interfaces
{
    public interface IStopLossMeta
    {
        /// <summary>
        /// Der Preis der eröffnung
        /// </summary>
        IPriceRecord Opening { get; set; }

        /// <summary>
        /// Das niedrigste Low
        /// </summary>
        IPriceRecord PreviousLow { get; }

        /// <summary>
        /// Das aktuell höchste Low
        /// </summary>
        IPriceRecord LocalLow { get; }

        /// <summary>
        /// Das High seit Eröffnung
        /// </summary>
        IPriceRecord High { get; }


    }
}