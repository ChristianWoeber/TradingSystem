using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Database.Models;
using HelperLibrary.Interfaces;

namespace TradingSystemTests.Models
{
    public class TestPortfolio : IPortfolio
    {
        private readonly DateTime? _lastAsOf;
        private readonly List<Transaction> _items = new List<Transaction>();

        public TestPortfolio()
        {

        }

        public TestPortfolio(IEnumerable<Transaction> items, DateTime? lastAsOf)
        {
            _lastAsOf = lastAsOf;
            _items.AddRange(items);
        }

        public IEnumerator<Transaction> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Transaction this[int key]
        {
            get
            {
                var dic = _items.ToDictionary(x => x.SecurityId);
                return dic.ContainsKey(key) ? dic[key] : null;

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
            return _items.Count > 0;
        }

        /// <summary>
        /// das Flag das angibt, ob das CurrentPortfolio bereits initialisiert wurde
        /// </summary>
        public bool IsInitialized => _lastAsOf != null;
    }
}