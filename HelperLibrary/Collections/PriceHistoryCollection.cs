using HelperLibrary.Database.Models;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HelperLibrary.Extensions;
using System.Collections;
using HelperLibrary.Database;
using HelperLibrary.Calculations;
using HelperLibrary.Database.Interfaces;
using System;
using HelperLibrary.Interfaces;

namespace HelperLibrary.Collections
{
    /// <summary>
    /// Hilfsklasse die den YahooDataRecord beliebig erweitert
    /// </summary>
    public class PriceHistoryItem
    {
        /// <summary>
        /// Standard Konstruktor
        /// </summary>
        /// <param name="dbRecord">der Db Record</param>
        public PriceHistoryItem(YahooDataRecord dbRecord)
        {
            DbRecord = dbRecord;
        }

        /// <summary>
        /// Die tägliche Veränderung des Prices für das PriceDate 20.01.2016 bspw ist es die Veränderung von 19.01 auf 20.01 und hat als DateTime den 20.01
        /// </summary>
        public decimal DailyReturn { get; set; }

        /// <summary>
        /// Der Deb Record der Im Konstruktor injected wird
        /// </summary>
        public YahooDataRecord DbRecord { get; private set; }
    }

    public enum PriceHistoryOption
    {
        PreviousItem,
        NextItem
    }

    /// <summary>
    /// Price History Collection - Enthält Berechnungen zur PriceHistory und gibt die Items zurück
    /// </summary>
    public class PriceHistoryCollection : IEnumerable<ITradingRecord>, IPriceHistoryCollection
    {
        #region Items and LookUp

        /// <summary>
        /// der Backing Storage für die Items die im Enumerator zrückgegeben werden
        /// </summary>
        private readonly ObservableCollection<ITradingRecord> _items = new ObservableCollection<ITradingRecord>();


        #endregion

        #region Private Members

        private ITradingRecord _first => _items != null && _items.Count > 0 ? _items?[0] : null;
        private ITradingRecord _last => _items != null && _items.Count > 0 ? _items?[Count - 1] : null;
        private CalculationContext _calculationContext;

        #endregion



        #region Public Members
        /// <summary>
        /// Return the First Item in the Enumeration
        /// </summary>
        public ITradingRecord FirstItem => _first;

        /// <summary>
        /// Return the Last Item in the Enumeration
        /// </summary>
        public ITradingRecord LastItem => _last;



        /// <summary>
        /// Zugriff auf den Calculation Context
        /// </summary>
        public CalculationContext Calc => _calculationContext;




        internal void Clear()
        {
            _items.Clear();

        }

        internal void Add(ITradingRecord item)
        {
            if (item != null && item.Asof > DateTime.MinValue)
            {
                if (item.AdjustedPrice == decimal.Zero || item.Price == decimal.Zero)
                    return;

                //add the item to the history collection
                _items.Add(item);

                if (_items.Count == 1)
                    return;

                //ich füge immer das vorherige und das aktuelle item ein, davon rechne ich den return
                _calculationContext.AddDailyReturn(_items[_items.Count - 2], _items[_items.Count - 1]);

                //das arithmetische Mittel bereits beim Einfügern mit berechnen
                _calculationContext.CalcArithmeticMean(item);
            }
        }

        internal void AddRange(IEnumerable<ITradingRecord> data)
        {
            foreach (var item in data)
                Add(item);
        }

        private decimal GetDailyReturns(ITradingRecord from, ITradingRecord to)
        {
            return _calculationContext.GetDailyReturn(from, to);
        }

        /// <summary>
        /// Returns the Count of the items Collection
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Returns the SecurityId of the underlying Security
        /// </summary>
        public int SecurityId => FirstItem?.SecurityId ?? -1;


        #endregion

        #region Constructor

        public PriceHistoryCollection(IEnumerable<ITradingRecord> dbRecords)
        {
            _calculationContext = new CalculationContext(this);
            //add the items to the Observable Collection this can be bound to the UI
            AddRange(dbRecords);


        }



        public void Delete(ITradingRecord selectedRecord)
        {
            if (_items.Contains(selectedRecord))
                _items.Remove(selectedRecord);
        }

        /// <summary>
        /// Clears the old items and loads the complete collecton anew
        /// </summary>
        public void Refresh(int secid)
        {

            if (_items.Count > 0)
                _items.Clear();

            var newItems = DataBaseQueryHelper.GetSinglePriceHistory(secid);
            AddRange(newItems.ToList());
        }


        #endregion

        #region Methods

        /// <summary>
        /// Indexer return the last item found
        /// </summary>
        /// <param name="key">DateTime</param>
        /// <returns></returns>
        public ITradingRecord this[DateTime key]
        {
            get
            {
                return Get(key);
            }
        }

        public IEnumerable<ITradingRecord> Range(DateTime? from, DateTime? to, PriceHistoryOption option = PriceHistoryOption.PreviousItem)
        {
            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.MaxValue;

            return _items.Where(x => x.Asof >= start && x.Asof <= end);
        }

        public PriceHistoryCollection RangeHistory(int from, int? to = null, PriceHistoryOption option = PriceHistoryOption.PreviousItem)
        {
            var start = from == 0 ? FirstItem : Get(from);
            var end = to == null ? LastItem : Get(to.Value);

            return new PriceHistoryCollection(_items.Where(x => x.Asof >= start.Asof && x.Asof <= end.Asof));
        }

        public PriceHistoryCollection RangeHistory(DateTime? from, DateTime? to, PriceHistoryOption option = PriceHistoryOption.PreviousItem)
        {
            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.MaxValue;

            return new PriceHistoryCollection(_items.Where(x => x.Asof >= start && x.Asof <= end));
        }

        private const int CANCELLATION_COUNT = 15;

        private int _count;


        public ITradingRecord Get(DateTime asof, PriceHistoryOption option = PriceHistoryOption.PreviousItem, int count = 0)
        {
            if (asof <= DateTime.MinValue)
                return null;

            _count = count;

            // Wenn nach 30 Versuchen kein Preis gefunden wurde breche ich ab und gebe null zurück
            if (_count == CANCELLATION_COUNT)
                return null;

            var record = _items.FirstOrDefault(x => x.Asof == asof);

            if (record != null)
                return record;

            _count++;

            switch (option)
            {
                case PriceHistoryOption.PreviousItem:
                    return Get(asof.AddDays(-1), option, _count);
                case PriceHistoryOption.NextItem:
                    return Get(asof.AddDays(1), option, _count); ;
            }

            return null;

        }

        public ITradingRecord Get(int index, PriceHistoryOption option = PriceHistoryOption.PreviousItem, int count = 0)
        {
            if (index <= 0)
                return null;

            _count = count;

            // Wenn nach 30 Versuchen kein Preis gefunden wurde breche ich ab und gebe null zurück
            if (_count == CANCELLATION_COUNT)
                return null;

            var record = _items[index];

            if (record != null)
                return record;


            switch (option)
            {
                case PriceHistoryOption.PreviousItem:
                    return Get(index--, option, _count);
                case PriceHistoryOption.NextItem:
                    return Get(index++, option, _count); ;
            }

            return null;

        }


        #endregion


        #region Enumerator

        public IEnumerator<ITradingRecord> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region CompareTo

        public int CompareTo(ITradingRecord other)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ITradingRecord> EnumMonthlyUltimoItems()
        {
            foreach (var item in _items.Where(x => x.Asof.IsBusinessDayUltimo()))            
               yield return item;
        }



        #endregion

    }
}
