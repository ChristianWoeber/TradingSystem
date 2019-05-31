using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    public interface IHistogrammCollection
    {
        /// <summary>
        /// Das Maximum
        /// </summary>
        IPeriodeResult Maximum { get; }

        /// <summary>
        /// Das minimum
        /// </summary>
        IPeriodeResult Minimum { get; }

        /// <summary>
        /// die Periode für die Rollierende Berechnung
        /// </summary>
        int PeriodeInYears { get;  }

        /// <summary>
        /// der Count
        /// </summary>
        int Count { get; }

        /// <summary>
        /// die relative Häufigkeit der Klasse
        /// </summary>
        decimal RelativeFrequency { get; }

        /// <summary>
        /// Gibt die Daten für das Histogramm und für die aktuelle Klassenbreite zurück
        /// </summary>
        /// <returns></returns>
        IEnumerable<IHistogrammCollection> EnumHistogrammClasses();
    }
}