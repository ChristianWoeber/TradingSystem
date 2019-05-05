using System;
using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Das Interface für die INfo über die Summe der postiven und negativen täglichen Returns
    /// </summary>
    public interface IAbsoluteLossesAndGainsMetaInfo
    {
        /// <summary>
        /// die Summe der negativen Returns
        /// </summary>
        decimal AbsoluteLoss { get; }
        /// <summary>
        /// die Summe der negativen Returns
        /// </summary>
        decimal AbsoluteGain { get; }
        /// <summary>
        /// die Summe der Returns
        /// </summary>
        decimal AbsoluteSum { get; }

        /// <summary>
        /// Die Liste der Records, die immer wieder nachgezogen werden
        /// </summary>
        List<IDailyReturnMetaInfo> Records { get; }

        /// <summary>
        /// Methode zum updaten
        /// </summary>
        /// <param name="dailyReturnMetaInfo">die MetaInfo</param>
        void Update(IDailyReturnMetaInfo dailyReturnMetaInfo);

        int DaysSettings { get; }

    }
}