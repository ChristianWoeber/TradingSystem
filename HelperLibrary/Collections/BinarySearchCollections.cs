using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Collections
{
    public class BinarySearchCollection<TKey, TValue> : IEnumerable<BinarySearchCollection<TKey, TValue>.KeyValuePair> where TKey : IComparable
    {
        private readonly SynchronizedCollection<KeyValuePair> _items = new SynchronizedCollection<KeyValuePair>();

        private readonly object _sortLock = new object();

        private bool _isDirty;

        public BinarySearchCollection()
        {

        }

        public BinarySearchCollection(IEnumerable<KeyValuePair> items)
        {
            foreach (var item in items)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// Ruft die Anzahl der Elemente in der threadsicheren Auflistung ab.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gibt an, ob der Indexer bei einem nicht gefundenen Key den Value mit dem letzten vorherigen Key zurückgibt
        /// </summary>
        public BinarySearchOption DefaultQueryOption { get; set; } = BinarySearchOption.GetLastIfNotFound;

        /// <summary>
        /// Gibt das erste Item der Auflistung zurück
        /// </summary>
        public KeyValuePair FirstItem
        {
            get
            {
                EnsureSorted();
                return _items.Count == 0 ? null : _items[0];
            }
        }

        public IList<TValue> Items => _items.Select(x => x.Value).ToList();
        /// <summary>
        /// Gibt an, ob die Collection geändert werden kann, oder nicht
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Gibt das letzte Item der Auflistung zurück
        /// </summary>
        public KeyValuePair LastItem
        {
            get
            {
                EnsureSorted();
                return _items.Count == 0 ? null : _items[_items.Count - 1];
            }
        }

        /// <summary>
        /// Der Value ist nur für debug zwecke und gibt an, wie oft der tale bereits sortiert wurde
        /// </summary>
        public long NumberOfSorts { get; private set; }

        /// <summary>
        /// Gibt den Value zu einem Key zurück
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                var kvp = Get(key, DefaultQueryOption);
                return kvp == null ? default(TValue) : kvp.Value;
            }
        }

        /// <summary>
        /// Gibt den Value zu einem bestimmten key zurück
        /// </summary>
        /// <param name="key"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public TValue this[TKey key, BinarySearchOption option]
        {
            get
            {
                var kvp = Get(key, option);
                return kvp == null ? default(TValue) : kvp.Value;
            }
        }

        /// <summary>
        ///  Fügt der Auflistung ein Element hinzu.
        /// </summary>
        /// <param name="kvp"></param>
        public void Add(KeyValuePair kvp)
        {
            if (Count == 0)
            {
                _items.Add(kvp);
                _isDirty = false;
            }
            else if (_isDirty)
            {
                _items.Add(kvp);
            }
            else if (kvp.Key.CompareTo(LastItem.Key) > 0)
            {
                _items.Add(kvp);
            }
            else if (kvp.Key.CompareTo(FirstItem.Key) < 0)
            {
                _items.Insert(0, kvp);
            }
            else
            {
                _items.Add(kvp);
                _isDirty = true;
            }
        }

        /// <summary>
        ///  Fügt der Auflistung eine auflistung von Elementen hinzu.
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<KeyValuePair> items)
        {
            foreach (var i in items.OrderBy(x => x.Key))
                Add(i);
        }

        /// <summary>
        ///  Fügt der Auflistung ein Element hinzu.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair(key, value));
        }

        /// <summary>
        /// Klonen eines Itemsbereiches
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public BinarySearchCollection<TKey, TValue> Clone(TKey from, TKey to)
        {
            return new BinarySearchCollection<TKey, TValue>(Range(from, to));
        }

        /// <summary>
        /// Klonen eines Itemsbereiches
        /// </summary>
        /// <returns></returns>
        public BinarySearchCollection<TKey, TValue> Clone()
        {
            return new BinarySearchCollection<TKey, TValue>(_items);
        }

        /// <summary>
        /// Klonen der Items in einem Indexbereich
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public BinarySearchCollection<TKey, TValue> Clone(int start, int count)
        {
            return new BinarySearchCollection<TKey, TValue>(Range(start, start + count));
        }

        /// <summary>
        /// Gibt die Row für ein bestimmtem key zurück, oder null falls diese nicht existiert
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public KeyValuePair Get(TKey key)
        {
            return Get(key, BinarySearchOption.GetNextIfNotFound);
        }

        /// <summary>
        /// Gibt die Row für ein bestimmtem key zurück
        /// </summary>
        /// <param name="key"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public KeyValuePair Get(TKey key, BinarySearchOption option)
        {
            var idx = IndexOf(key, option);
            return Get(idx, option);
        }

        /// <summary>
        /// Gibt die Row für ein bestimmtem key zurück, oder null falls diese nicht existiert
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public KeyValuePair Get(int idx, BinarySearchOption option)
        {
            EnsureSorted();
            return idx < 0 || idx >= _items.Count ? null : _items[idx];
        }

        public IEnumerator<KeyValuePair> GetEnumerator()
        {
            EnsureSorted();
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gibt den Index des Keys zurück
        /// </summary>
        /// <param name="key"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public int IndexOf(TKey key, BinarySearchOption options)
        {
            if (_items.Count == 0)
                return -1;

            return TrySearchParition(key, 0, _items.Count - 1, options);
        }

        /// <summary>
        /// Gibt die Zeilen in dem Angegebenen Bereich zurück
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair> Range(TKey from, TKey to)
        {
            if (_items.Count == 0)
                yield break;

            if (from.CompareTo(to) > 0)
            {
                var tmp = to;
                to = from;
                from = tmp;
            }

            int fromIndex;
            int toIndex;

            if (from == null || from.CompareTo(FirstItem.Key) < 0)
                fromIndex = 0;
            else
                fromIndex = IndexOf(from, BinarySearchOption.GetNextIfNotFound);

            if (to == null || to.CompareTo(LastItem.Key) > 0)
                toIndex = _items.Count - 1;
            else
                toIndex = Math.Min(_items.Count, IndexOf(to, BinarySearchOption.GetLastIfNotFound));

            foreach (var r in Range(fromIndex, toIndex))
                yield return r;
        }

        /// <summary>
        /// Gibt die items in dem angegebenen Bereich zurück
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair> Range(int? from, int? to)
        {
            if (_items.Count == 0)
                yield break;

            EnsureSorted();
            if (from == null || from < 0)
                from = 0;

            if (to == null || to >= _items.Count || to < 0)
                to = _items.Count - 1;

            for (var i = from; i <= to; i++)
                yield return _items[i.Value];
        }

        /// <summary>
        /// Garantiert, dass die Collection sortiert ist
        /// </summary>
        private void EnsureSorted()
        {
            lock (_sortLock)
            {
                if (!_isDirty)
                    return;

                Sort();
                _isDirty = false;
            }
        }

        /// <summary>
        /// Quicksort algorithmus
        /// </summary>
        /// <param name="leftIndex"></param>
        /// <param name="rightIndex"></param>
        private void QuickSort(int leftIndex, int rightIndex)
        {
            if (leftIndex >= rightIndex)
                return;

            var pivotIndex = SortParition(leftIndex, rightIndex);

            if (pivotIndex > 1)
                QuickSort(leftIndex, pivotIndex - 1);

            if (pivotIndex + 1 < rightIndex)
                QuickSort(pivotIndex + 1, rightIndex);
        }

        /// <summary>
        /// Sortiert die Collection
        /// </summary>
        private void Sort()
        {
            if (_items.Count == 1)
                return;

            lock (_sortLock)
            {
                NumberOfSorts++;
                QuickSort(0, _items.Count - 1);
            }
        }

        /// <summary>
        /// Sortiert eine Partition im für Quicksort
        /// </summary>
        /// <param name="leftIndex"></param>
        /// <param name="rightIndex"></param>
        /// <returns></returns>
        private int SortParition(int leftIndex, int rightIndex)
        {
            var pivotValue = _items[leftIndex];

            while (true)
            {
                //Suche von links ein Element, welches größer als das Pivotelement ist
                while (_items[leftIndex].Key.CompareTo(pivotValue.Key) < 0)
                    leftIndex++;

                //Suche von rechts ein Element, welches kleiner als das Pivotelement ist
                while (_items[rightIndex].Key.CompareTo(pivotValue.Key) > 0)
                    rightIndex--;

                if (_items[rightIndex].Key.CompareTo(pivotValue.Key) == 0 &&
                    _items[leftIndex].Key.CompareTo(pivotValue.Key) == 0)
                    leftIndex++;

                // tausche daten[i] mit daten[j]
                if (leftIndex < rightIndex)
                {
                    var temp = _items[rightIndex];
                    _items[rightIndex] = _items[leftIndex];
                    _items[leftIndex] = temp;
                }
                else
                {
                    return rightIndex;
                }
            }
        }

        /// <summary>
        ///     Search the key in the collection recursive
        /// </summary>
        /// <param name="key"></param>
        /// <param name="leftIndex"></param>
        /// <param name="rightIndex"></param>
        /// <param name="options"></param>
        /// <returns>The index of the array, or -1 if not found</returns>
        private int TrySearchParition(TKey key, int leftIndex, int rightIndex, BinarySearchOption options)
        {
            EnsureSorted();
            while (true)
            {
                if (leftIndex > rightIndex)
                {
                    switch (options)
                    {
                        case BinarySearchOption.GetLastIfNotFound:
                            return leftIndex > 0 ? leftIndex - 1 : -1;
                        case BinarySearchOption.GetInvalidIfNotFound:
                            return -1;
                        case BinarySearchOption.GetNextIfNotFound:
                            return leftIndex;
                        case BinarySearchOption.GetExceptionIfNotFound:
                            throw new KeyNotFoundException("Es konnte kein Element mit dem Key \"" + key + "\" gefunden werden");
                        default:
                            throw new ArgumentOutOfRangeException(nameof(options), options, null);
                    }
                }


                var pivotIndex = leftIndex + ((rightIndex - leftIndex) / 2);
                var cmpResult = _items[pivotIndex].Key.CompareTo(key);

                if (cmpResult == 0)
                    return pivotIndex;

                if (cmpResult > 0)
                {
                    rightIndex = pivotIndex - 1;
                    continue;
                }
                leftIndex = pivotIndex + 1;
            }
        }

        public sealed class KeyValuePair : IComparable<KeyValuePair>, IComparable<TKey>
        {
            public KeyValuePair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public int CompareTo(KeyValuePair other)
            {
                return other == null ? 1 : CompareTo(other.Key);
            }

            public int CompareTo(TKey other)
            {
                if (Equals(Key, default(TKey)) && Equals(other, default(TKey)))
                    return 0;

                if (Equals(Key, default(TKey)))
                    return -1;

                return Key.CompareTo(other);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as KeyValuePair);
            }

            private bool Equals(KeyValuePair other)
            {
                return EqualityComparer<TKey>.Default.Equals(Key, other.Key);
            }

            public override int GetHashCode()
            {
                return EqualityComparer<TKey>.Default.GetHashCode(Key);
            }

            public TKey Key { get; }

            public TValue Value { get; set; }

            public override string ToString()
            {
                return Key + ": " + Value;
            }
        }
    }
}


