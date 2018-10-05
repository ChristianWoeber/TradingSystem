using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Calculations
{
    public class DrawdownItem
    {
        public ITradingRecord Start { get; set; }
        public ITradingRecord End { get; set; }
        public decimal Drawdown { get; set; }
    }
}