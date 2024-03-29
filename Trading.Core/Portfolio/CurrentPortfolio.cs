﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Portfolio
{
    public class CurrentPortfolio : IPortfolio
    {
        private readonly DateTime? _lastAsOf;
        [JsonProperty(TypeNameHandling = TypeNameHandling.None)]
        private readonly List<ITransaction> _items = new List<ITransaction>();

        public CurrentPortfolio()
        {

        }

        public CurrentPortfolio(IEnumerable<ITransaction> items, DateTime? lastAsOf)
        {
            _lastAsOf = lastAsOf;
            _items.AddRange(items);
        }

        public IEnumerator<ITransaction> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [JsonIgnore]
        public ITransaction this[int key]
        {
            get
            {
                var dic = _items.ToDictionary(x => x.SecurityId);
                return dic.TryGetValue(key, out var transaction) ? transaction : null;
            }
        }


        /// <summary>
        /// Damit soll sichergestellt werden dass nur pro stichtag einmal das
        /// _currentPortfolio erstellt wird und nicht jedesmal wenn ich darauf zugreife
        /// </summary>
        /// <param name="asof">der Betrachtungszeitpunkt</param>
        /// <returns></returns>
        public bool HasItems(DateTime asof)
        {
            //Dann erstelle ich es aufjedenfall neu
            if (_lastAsOf == null || asof > _lastAsOf)
                return false;
            //sonst nur wenn es noch keine Einträge hat
            return _items?.Count > 0;
        }

        /// <summary>
        /// das Flag das angibt, ob das CurrentPortfolio bereits initialisiert wurde
        /// </summary>
        public bool IsInitialized => _lastAsOf != null;

        public void Add(ITransaction item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(ITransaction item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(ITransaction[] array, int arrayIndex)
        {
            _items.CopyTo(array);
        }

        public bool Remove(ITransaction item)
        {
            return _items.Remove(item);
        }

        public int Count => _items.Count;

        public bool IsReadOnly { get; }

    }
}