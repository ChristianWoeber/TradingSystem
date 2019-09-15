using System;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    public class PeriodeResult : IPeriodeResult
    {
        public PeriodeResult(int rollingPeriodeInYears, decimal performance,decimal performanceCompound, DateTime form, DateTime to)
        {
            RollingPeriodeInYears = rollingPeriodeInYears;
            Performance = performance;
            PerformanceCompound = performanceCompound;
            Form = form;
            To = to;
        }

        /// <summary>
        /// Der Wert für die Periode in Jahren (1,3,5,10 etc..)
        /// </summary>
        public int RollingPeriodeInYears { get; }

        /// <summary>
        /// die performacne in dem Zeitraunm
        /// </summary>
        public decimal Performance { get; }

        /// <summary>
        /// die Performance p.a.
        /// </summary>
        public decimal PerformanceCompound { get; }

        /// <summary>
        /// Startzeitpunk
        /// </summary>
        public DateTime Form { get; }

        /// <summary>
        /// Endzeotpunkt
        /// </summary>
        public DateTime To { get; }

    }
}