using System;

namespace Trading.DataStructures.Interfaces
{
    public interface IPeriodeResult
    {
        /// <summary>
        /// Der Wert für die Periode in Jahren (1,3,5,10 etc..)
        /// </summary>
        int RollingPeriodeInYears { get; }

        /// <summary>
        /// die performacne in dem Zeitraunm
        /// </summary>
        decimal Performance { get; }

        /// <summary>
        /// die Performance p.a.
        /// </summary>
        decimal PerformanceCompound { get; }

        /// <summary>
        /// Startzeitpunk
        /// </summary>
        DateTime Form { get; }

        /// <summary>
        /// Endzeotpunkt
        /// </summary>
        DateTime To { get; }
    }
}