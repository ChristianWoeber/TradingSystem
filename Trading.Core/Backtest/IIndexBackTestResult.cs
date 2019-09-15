using System;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Backtest
{
    public interface IIndexBackTestResult : IExposureSettings
    {
        DateTime Asof { get; set; }

        decimal SimulationNav { get; set; }

        decimal IndexLevel { get; set; }
    }
}