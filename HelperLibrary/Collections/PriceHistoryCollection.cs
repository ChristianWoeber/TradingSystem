using HelperLibrary.Database.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections;
using HelperLibrary.Database;
using HelperLibrary.Calculations;
using System;
using Trading.DataStructures.Interfaces;
using Trading.DataStructures.Enums;
using HelperLibrary.Extensions;

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
        public PriceHistoryItem(TradingRecord dbRecord)
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
        public TradingRecord DbRecord { get; private set; }
    }



    /// <summary>
    /// Price History Collection - Enthält Berechnungen zur PriceHistory und gibt die Items zurück
    /// </summary>
    public class PriceHistoryCollection : IEnumerable<ITradingRecord>, IPriceHistoryCollection
    {
        private readonly bool _calcMovingLows;


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
        public ICalculationContext Calc => (ICalculationContext)_calculationContext;



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
                _calculationContext.CalcArithmeticMean(item, _items.Count);

                if (_calcMovingLows)
                    _calculationContext.CalcMovingLows(item, _items.Count);
            }
        }

        internal void AddRange(IEnumerable<ITradingRecord> data)
        {
            foreach (var item in data)
                Add(item);
        }


        public decimal GetDailyReturn(ITradingRecord record)
        {
            if(record==null)
                return decimal.MinusOne;
            return _calculationContext.TryGetDailyReturn(record.Asof, out var dailyReturn) ? dailyReturn : decimal.MinusOne;
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

        public PriceHistoryCollection(IEnumerable<ITradingRecord> tradingRecords, bool calcMovingLows = false, int movingLowsPeriode = 150)
        {
            _calcMovingLows = calcMovingLows;
            MovingDays = movingLowsPeriode;
            _calculationContext = new CalculationContext(this);
            //add the items to the Observable Collection this can be bound to the UI
            AddRange(tradingRecords);
        }

        internal int MovingDays;

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
        public ITradingRecord this[DateTime key] => Get(key);

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

        public IEnumerable<Tuple<DateTime, LowMetaInfo>> EnumLows()
        {
            foreach (var low in _calculationContext.EnumLows())
                yield return low;
        }


        public bool TryGetLowMetaInfo(DateTime currentDate, out LowMetaInfo info)
        {
            return _calculationContext.TryGetLastLowItem(currentDate, out info);
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
