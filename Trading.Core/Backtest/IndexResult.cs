using System;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Backtest
{
    public class IndexResult : IIndexBackTestResult
    {
        public IndexResult()
        {

        }

        public IndexResult(IExposureSettings receiver)
        {
            MaximumAllocationToRisk = receiver.MaximumAllocationToRisk;
            MinimumAllocationToRisk = receiver.MinimumAllocationToRisk;
            Asof = ((IIndexBackTestResult)receiver).Asof;
            SimulationNav = ((IIndexBackTestResult)receiver).SimulationNav;
            IndexLevel = ((IIndexBackTestResult)receiver).IndexLevel;
            TradingDay = receiver.TradingDay;

        }

        public decimal MaximumAllocationToRisk { get; set; }
        public decimal MinimumAllocationToRisk { get; set; }
        public string IndicesDirectory { get; set; }
        public DayOfWeek TradingDay { get; set; }

        /// <summary>
        /// Der Index der für die Steuerung der Aktienquote verwender werden soll
        /// </summary>
        public IndexType IndexType { get; set; }

        public DateTime Asof { get; set; }

        public decimal SimulationNav { get; set; }

        public decimal IndexLevel { get; set; }

        public override string ToString()
        {
            return
                $"{Asof} {nameof(SimulationNav)}: {SimulationNav} {nameof(MaximumAllocationToRisk)}: {MaximumAllocationToRisk} {nameof(IndexLevel)} : {IndexLevel}";
        }
    }
}