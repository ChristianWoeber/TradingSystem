using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.Utils
{
    public class PriceHistorySettings : IPriceHistoryCollectionSettings
    {
        public PriceHistorySettings(int movingAverageLengthInDays = 0, int movingDaysVolatility = 250, int movingDaysAbsoluteLosses = 60)
        {
            MovingAverageLengthInDays = movingAverageLengthInDays;
            MovingDaysVolatility = movingDaysVolatility;
            MovingDaysAbsoluteLossesGains = movingDaysAbsoluteLosses;
        }

        /// <summary>
        /// die Länge des Moving Averages
        /// </summary>
        public int MovingAverageLengthInDays { get; set; }

        /// <summary>
        /// die "Länge" der Volatilität
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