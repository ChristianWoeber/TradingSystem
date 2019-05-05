namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Das Interface das der PriceHistoryCollection mitübergeben werden kann
    /// </summary>
    public interface IPriceHistoryCollectionSettings
    {
        /// <summary>
        /// die Länge des Moving Averages
        /// </summary>
        int MovingAverageLengthInDays { get; set; }

        /// <summary>
        /// die "Länge" der Volatilität
        /// </summary>
        int MovingDaysVolatility { get; set; }

        /// <summary>
        /// Der Name des Wertpapiers
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// die "Länge" der Periode für die Betrachtung der kumulierten Losses und Gains
        /// versuche mit diesem Parameter die Stablilität des Trends zu bewerten
        /// </summary>
        int MovingDaysAbsoluteLossesGains { get; set; }

    }
}