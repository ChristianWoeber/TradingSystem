using System;
using System.Collections.Generic;
using HelperLibrary.Collections;

namespace HelperLibrary.Calculations
{
    public partial class CalculationContext
    {
        public abstract class LastItemDictionaryBase<TValue> : BinarySearchCollection<DateTime, TValue> where TValue : class
        {
            /// <summary>
            /// Das zuletzt eingefügte KVP
            /// </summary>
            private KeyValuePair<DateTime, TValue> _lastKeyValuePair;

            /// <summary>
            /// überschreibe hier die add methode
            /// und merke mir den zuletzt eingefügten wert
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public new void Add(DateTime key, TValue value)
            {
                _lastKeyValuePair = new KeyValuePair<DateTime, TValue>(key, value);
                base.Add(key, value);
            }


            public bool TryGetLastItem(DateTime key, out TValue lastMetaInfo)
            {
                lastMetaInfo = null;
                if (Count == 0)
                {
                    return false;
                }

                if (_lastKeyValuePair.Key <= DateTime.MinValue)
                    return false;

                //Dadurch erspare ich mir beim einfüllen der Daten unnötige Rekursionen
                if (_lastKeyValuePair.Key <= key)
                {
                    lastMetaInfo = _lastKeyValuePair.Value;
                    return true;
                }

                //Wenn keinen Match in der Binary Search Collection habe return ich false
                var match = base.Get(key, BinarySearchOption.GetLastIfNotFound);
                if (match == null)
                    return false;
                //sonst das item
                lastMetaInfo = match.Value;
                return true;
            }
        }
    }
}