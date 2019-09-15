using System;
using System.Collections.Generic;
using Trading.Calculation;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Exposure
{
    public interface IExposureProvider
    {
        /// <summary>
        /// Methode zum Berechnen des maximalen Exposures
        /// </summary>
        /// <param name="asof"></param>
        void CalculateMaximumExposure(DateTime asof);

        /// <summary>
        /// die anzahle der Steps bis ich das minimum erreiche
        /// </summary>
        int NumberOfSteps { get; set; }

        /// <summary>
        /// enumeriert mit die metaInfos zu jedem vorhanden Zeitfenster
        /// </summary>
        /// <returns></returns>
        IEnumerable<(DateTime dateTime, ILowMetaInfo metaInfo)> EnumLows();

        /// <summary>
        /// Berechnet das Ergebnis der Index Simulation
        /// </summary>
        /// <param name="start"></param>
        void CalculateIndexResult(DateTime start);

        /// <summary>
        /// Gibt das Exposure nach aussen
        /// </summary>
        /// <param name="start"></param>
        IExposureSettings GetExposure();
    }
}