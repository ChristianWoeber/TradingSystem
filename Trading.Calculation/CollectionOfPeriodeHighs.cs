using System;
using System.Collections;
using System.Collections.Generic;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    /// <summary>
    /// die Collection die die neuen Hochs fürht
    /// </summary>
    public class CollectionOfPeriodeHighs : ICollectionOfPeriodeHighs
    {
        private readonly List<ITradingRecord> _items = new List<ITradingRecord>();

        public CollectionOfPeriodeHighs()
        {

        }

        public CollectionOfPeriodeHighs(ICollectionOfPeriodeHighs newHighsCollection)
        {
            AddRange(newHighsCollection);
        }

        public ITradingRecord First => Count > 0 ? _items[0] : null;

        public ITradingRecord Last => Count > 0 ? _items[_items.Count - 1] : null;

        public int Count => _items.Count;

        public void Add(ITradingRecord record)
        {
            _items.Add(record);
        }

        public void AddRange(IEnumerable<ITradingRecord> highs)
        {
            foreach (var tradingRecord in highs)
            {
                Add(tradingRecord);
            }
        }

        public void Update(ITradingRecord newRecord)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// entfernt den ersten Eintrag der Collection
        /// </summary>
        public void Shift()
        {
            if (_items.Count <= 0)
                return;
            _items.RemoveAt(0);
        }


        /// <summary>Gibt einen Enumerator zurück, der die Auflistung durchläuft.</summary>
        /// <returns>Ein Enumerator, der zum Durchlaufen der Auflistung verwendet werden kann.</returns>
        public IEnumerator<ITradingRecord> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>Gibt einen Enumerator zurück, der eine Auflistung durchläuft.</summary>
        /// <returns>Ein <see cref="T:System.Collections.IEnumerator" />-Objekt, das zum Durchlaufen der Auflistung verwendet werden kann.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


    }
}