using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    public interface ILowMetaInfo
    {
        /// <summary>
        /// der erste Wert der Betrachtungsperiode
        /// </summary>
        ITradingRecord First { get; }

        /// <summary>
        /// Das Low der Betrachtungsperiode
        /// </summary>
        ITradingRecord Low { get; }

        /// <summary>
        /// Das High der Betrachtungsperiode
        /// </summary>
        ITradingRecord High { get; }

        /// <summary>
        /// Der Letzte Wert der Betrachtungsperiode
        /// </summary>
        ITradingRecord Last { get; }

        /// <summary>
        /// Alle Records des MovingFensters
        /// </summary>
        List<ITradingRecord> PeriodeRecords { get; }

        /// <summary>
        /// Gibt an ob es ein neus Low gibt
        /// </summary>
        bool HasNewLow { get; }

        /// <summary>
        /// Der Moving Average Periode
        /// </summary>
        decimal MovingAverage { get; }

        /// <summary>
        /// Die Veränderung des Moving Averages
        /// </summary>
        decimal MovingAverageDelta { get; set; }

        /// <summary>
        /// Gibt an ob die aktienquote erhöht oder gesenkt werden darf
        /// </summary>
        bool CanMoveToNextStep { get; set; }

        /// <summary>
        /// der Count der Angibt wieviele Neue Hochs in der Periode erreicht werden konnten
        /// und somit aussage kraft über die Trendstabilität widerspiegelt
        /// </summary>
        int NewHighsCount { get; set; }
    }
}