using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    /// <summary>
    /// Die MetaInfo des DrawDowns
    /// </summary>
    public class DrawdownMetaInfo
    {
        /// <summary>
        /// der Start
        /// </summary>
        public ITradingRecord Start { get; set; }

        /// <summary>
        /// Das Ende
        /// </summary>
        public ITradingRecord End { get; set; }

        /// <summary>
        /// Der berechnete Wert
        /// </summary>
        public decimal Drawdown { get; set; }
    }
}