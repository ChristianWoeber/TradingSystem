using System;
using System.Collections.Generic;
using HelperLibrary.Calculations;

namespace HelperLibrary.Trading.PortfolioManager
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
        IEnumerable<Tuple<DateTime, LowMetaInfo>> EnumLows();
    }
}