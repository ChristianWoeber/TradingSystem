using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Trading.Calculation.Extensions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;


namespace Trading.Calculation.Collections
{
    /// <summary>
    /// Price History Collection - Enthält Berechnungen zur PriceHistory und gibt die Items zurück
    /// </summary>
    public class PriceHistoryCollection : IPriceHistoryCollection
    {

        #region private Collections and LookUp

        /// <summary>
        /// der Backing Storage für die Items die im Enumerator zrückgegeben werden
        /// </summary>
        private readonly BinarySearchCollection<DateTime, ITradingRecord> _items = new BinarySearchCollection<DateTime, ITradingRecord>();


        #endregion

        #region Private Members

        private ITradingRecord _first => _items != null && _items.Count > 0 ? _items?.FirstItem.Value : null;
        private ITradingRecord _last => _items != null && _items.Count > 0 ? _items?.LastItem.Value : null;

        /// <summary>
        /// der Context für die diversen Berechungen
        /// </summary>
        private readonly ICalculationContext _calculationContext;

        #endregion

        #region Constructor

        private PriceHistoryCollection(IEnumerable<ITradingRecord> tradingRecords, IPriceHistoryCollectionSettings settings = null)
        {
            Settings = settings;
            _calculationContext = new CalculationContext(this);
            //add the items to the Observable Collection this can be bound to the UI
            AddRange(tradingRecords);
        }

        /// <summary>
        /// Methode zum erstellen der PriceHistoryCollection
        /// </summary>
        /// <param name="tradingRecords">die records</param>
        /// <param name="settings">die Einstellungen zur Collection</param>
        /// <returns></returns>
        public static IPriceHistoryCollection Create([NotNull]IEnumerable<ITradingRecord> tradingRecords, IPriceHistoryCollectionSettings settings = null)
        {
            return new PriceHistoryCollection(tradingRecords, settings);
        }



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
        public ICalculationContext Calc => _calculationContext;

        /// <summary>
        /// Das Low der PriceHistory
        /// </summary>
        public ITradingRecord Low { get; internal set; }

        /// <summary>
        /// Das High der PriceHistory
        /// </summary>
        public ITradingRecord High { get; internal set; }


        internal void Add(ITradingRecord item)
        {
            if (item == null || item.Asof <= DateTime.MinValue)
                return;

            if (item.AdjustedPrice == decimal.Zero || item.Price == decimal.Zero)
                return;

            //Low und High der Collection mitziehen
            if (item.AdjustedPrice > High?.AdjustedPrice)
                High = item;

            if (item.AdjustedPrice < Low?.AdjustedPrice)
                Low = item;

            //einfügen
            _items.Add(new BinarySearchCollection<DateTime, ITradingRecord>.KeyValuePair(item.Asof, item));

            //beim ersten Eintrag kann ich noch nichts berechnen
            if (_items.Count == 1)
            {
                //low & High initialisieren
                Low = new CalculationRecordMetaInfo(_items.FirstItem.Value);
                High = new CalculationRecordMetaInfo(Low);
                //last Ultimo initialiseren
                //_calculationContext.LastUltimoRecord = _items.FirstItem.Value;
                return;
            }

            //ich füge immer das vorherige und das aktuelle item ein, davon rechne ich den return
            _calculationContext.AddDailyReturn(_items.Get(_items.Count - 2, BinarySearchOption.GetLastIfNotFound)?.Value, _items.Get(_items.Count - 1, BinarySearchOption.GetLastIfNotFound).Value);

            //das arithmetische Mittel bereits beim Einfügern mit berechnen
            _calculationContext.CalcRunningArithmeticMean(item);

            //überprüfen ob der Collection settings mitgegeben wurden und danach die berechnnugen ausführen
            if (Settings?.MovingLowsLengthInDays > 0)
            {
                _calculationContext.CalcMovingLows(item);
            }

            if (Settings?.MovingDaysVolatility > 0)
            {
                if (item.Asof >= FirstItem.Asof.AddDays(Settings.MovingDaysVolatility))
                    _calculationContext.CalcMovingVola(item);
            }

            if (Settings?.MovingDaysAbsoluteLossesGains > 0)
            {
                if (item.Asof >= FirstItem.Asof.AddDays(Settings.MovingDaysAbsoluteLossesGains))
                    _calculationContext.CalcAbsoluteLossesAndGains(item);
            }

        }

        internal void AddRange(IEnumerable<ITradingRecord> data)
        {
            foreach (var item in data)
                Add(item);
        }


        public decimal GetDailyReturn(ITradingRecord record)
        {
            if (record == null)
                return decimal.MinusOne;
            return _calculationContext.TryGetDailyReturn(record.Asof, out var dailyReturn) ? dailyReturn.AbsoluteReturn : decimal.MinusOne;
        }

        /// <summary>
        /// Returns the Count of the items Collection
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Returns the SecurityId of the underlying Security
        /// </summary>
        public int SecurityId => FirstItem?.SecurityId ?? -1;

        /// <summary>
        /// die Settings für die Berechnung der Moving Averages bzw. der Vola
        /// </summary>
        public IPriceHistoryCollectionSettings Settings { get; }

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

            return _items.Range(start, end).Select(x => x.Value);
        }


        public PriceHistoryCollection RangeHistory(DateTime? from, DateTime? to, PriceHistoryOption option = PriceHistoryOption.PreviousItem)
        {
            var start = from ?? DateTime.MinValue;
            var end = to ?? DateTime.MaxValue;
            return new PriceHistoryCollection(_items.Range(start, end).Select(x => x.Value));
        }

        public IEnumerable<(DateTime dateTime, ILowMetaInfo metaInfo)> EnumLows()
        {
            return _calculationContext.EnumLows();
        }

        public IEnumerable<ITradingRecord> EnumMonthlyUltimoItems()
        {
            foreach (var item in _items.Where(x => x.Value.Asof.IsBusinessDayUltimo()))
                yield return item.Value;
        }

        public bool TryGetLowMetaInfo(DateTime currentDate, out ILowMetaInfo info)
        {
            return _calculationContext.TryGetLastLowInfo(currentDate, out info);
        }

        public bool TryGetVolatilityInfo(DateTime currentDate, out IMovingVolaMetaInfo info)
        {
            return _calculationContext.TryGetLastVolatilityInfo(currentDate, out info);
        }

        public bool TryGetAbsoluteLossesAndGains(DateTime currentDate, out IAbsoluteLossesAndGainsMetaInfo info)
        {
            return _calculationContext.TryGetLastAbsoluteLossAndGain(currentDate, out info);
        }


        public ITradingRecord Get(DateTime asof, PriceHistoryOption option = PriceHistoryOption.PreviousItem, int count = 0)
        {
            if (asof <= DateTime.MinValue)
                return null;

            //standardmäßig setze ich die option getLastIfNotFound
            var binaryOption = BinarySearchOption.GetLastIfNotFound;
            if (option == PriceHistoryOption.NextItem)
                binaryOption = BinarySearchOption.GetNextIfNotFound;

            //das gefunden Item
            var match = _items.Get(asof, binaryOption);
            //Bei der option previousday price immer noch eines zuückgehen, wenn der Preis vom selben Tag ist
            if (option == PriceHistoryOption.PreviousDayPrice && match?.Value.Asof >= asof)
                match = _items.Get(asof.AddDays(-1), BinarySearchOption.GetLastIfNotFound);
            return match?.Value;
        }

        public ITradingRecord Get(int index, PriceHistoryOption option = PriceHistoryOption.PreviousItem)
        {
            if (index < 0)
                return null;

            var match = _items.Get(index, option == PriceHistoryOption.PreviousItem
                ? BinarySearchOption.GetLastIfNotFound
                : BinarySearchOption.GetNextIfNotFound);
            return match.Value;

        }


        #endregion

        #region Enumerator

        public IEnumerator<ITradingRecord> GetEnumerator()
        {
            return _items.Select(x => x.Value).GetEnumerator();
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





        #endregion

    }
}
