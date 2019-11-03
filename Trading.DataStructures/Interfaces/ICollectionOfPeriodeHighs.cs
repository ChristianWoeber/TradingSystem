using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    public interface ICollectionOfPeriodeHighs : IEnumerable<ITradingRecord>
    {
        /// <summary>
        /// Das erste Hoch der Colleciton
        /// </summary>
        ITradingRecord First { get; }

        /// <summary>
        /// das letzte Hoch der Collection
        /// </summary>
        ITradingRecord Last { get; }

        /// <summary>
        /// die Anzahl an neuen Hochs in der Periode
        /// </summary>
        int Count { get; }

        /// <summary>
        /// fügt ein neues Hoch hinzu
        /// </summary>
        /// <param name="record"></param>
        void Add(ITradingRecord record);

        /// <summary>
        /// fügt mehrere neue Hochs hinzu
        /// </summary>
        /// <param name="highs"></param>
        void AddRange(IEnumerable<ITradingRecord> highs);

        /// <summary>
        /// entfernt den ersten Eintrag der Collection
        /// </summary>
        void Shift();
    }
}