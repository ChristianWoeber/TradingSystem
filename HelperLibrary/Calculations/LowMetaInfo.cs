using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Calculations
{
    /// <summary>
    /// Hilfsklasse für die Berechnung der neuen Lows
    /// </summary>
    public class LowMetaInfo
    {
        /// <summary>
        /// der erste Wert der Betrachtungsperiode
        /// </summary>
        public ITradingRecord First { get; }

        /// <summary>
        /// Das Low der Betrachtungsperiode
        /// </summary>
        public ITradingRecord Low { get; }

        /// <summary>
        /// Der Letzte Wert der Betrachtungsperiode
        /// </summary>
        public ITradingRecord Last { get; }

        /// <summary>
        /// Gibt an ob es ein neus Low gibt
        /// </summary>
        public bool HasNewLow { get; }

        public LowMetaInfo(ITradingRecord first, ITradingRecord low, ITradingRecord last, bool hasNewLow = true)
        {
            First = first;
            Low = low;
            Last = last;
            HasNewLow = hasNewLow;
        }

        public override string ToString()
        {
            return $"NewLow: {HasNewLow} LowDate: {Low.Asof.ToShortDateString()} lastDate: {Last.Asof.ToShortDateString()} firstDate: {First.Asof.ToShortDateString()}";
        }
    }
}