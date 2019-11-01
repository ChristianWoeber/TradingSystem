using Trading.DataStructures.Interfaces;

namespace Trading.Core.Settings
{
    public class PriceHistoryCollectionSettings : IPriceHistoryCollectionSettings
    {
        public PriceHistoryCollectionSettings(int movingAverageLengthInDays = 150, int movingDaysVolatility = 250, int movingDaysAbsoluteLosses = 60)
        {
            MovingLowsLengthInDays = movingAverageLengthInDays;
            MovingDaysVolatility = movingDaysVolatility;
            MovingDaysAbsoluteLossesGains = movingDaysAbsoluteLosses;
        }

        /// <summary>
        /// die Länge des Moving Averages in Tagen
        /// </summary>
        public int MovingLowsLengthInDays { get; set; }

        /// <summary>
        /// der Betrachtungszeitraum der Vola in Tagen
        /// </summary>
        public int MovingDaysVolatility { get; set; }

        /// <summary>
        /// Der Name des Wertpapiers
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// die "Länge" der Periode für die Betrachtung der kumulierten Losses und Gains
        /// versuche mit diesem Parameter die Stablilität des Trends zu bewerten
        /// </summary>
        public int MovingDaysAbsoluteLossesGains { get; set; }
    }
}