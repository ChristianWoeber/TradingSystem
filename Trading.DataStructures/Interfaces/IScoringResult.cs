using System;

namespace Trading.DataStructures.Interfaces
{
    public interface IScoringResult : IComparable
    {
        /// <summary>
        /// der Stichtag des Ergebnisses
        /// </summary>
        DateTime Asof { get; set; }

        /// <summary>
        /// Returns True if the Score is Valid
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Longest Performance evalulation
        /// </summary>
        decimal Performance250 { get; set; }

        /// <summary>
        /// 3 Months evalulation
        /// </summary
        decimal Performance90 { get; set; }

        /// <summary>
        /// One Month evalulation
        /// </summary
        decimal Performance30 { get; set; }

        /// <summary>
        /// Shortest evalulation
        /// </summary
        decimal Performance10 { get; set; }

        /// <summary>
        /// The Volatility of the last 250 Days
        /// </summary>
        decimal? Volatility { get; set; }

        /// <summary>
        /// der Score der sich aus den Berechnungen ergibt 
        /// </summary>
        decimal Score { get; }

        /// <summary>
        /// die LowMetaInfo zu dem Stichtag
        /// </summary>
        ILowMetaInfo LowMetaInfo { get; set; }

        /// <summary>
        /// Gibt an ob es positive kurz und mittelfristige Performance gibt
        /// </summary>
        bool HasPositiveShortToMidTermPerformance { get;  }
    }
}