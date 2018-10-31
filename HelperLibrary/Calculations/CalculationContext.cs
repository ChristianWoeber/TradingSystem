using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Collections;
using HelperLibrary.Database.Interfaces;
using HelperLibrary.Interfaces;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;


namespace HelperLibrary.Calculations
{
    /// <summary>
    /// Der Calculation Context, wird nur einmal pro Price History Collection erstellt wird
    /// </summary>
    public class CalculationContext : ICalculationContext
    {
        #region Private Member

        private readonly PriceHistoryCollection _priceHistory;
        private readonly CalculationHandler _handler;
        private decimal _arithmeticMean;
        private decimal _arithmeticMeanDailyReturns;

        #endregion

        #region Collections

        /// <summary>
        /// the Daily Returns
        /// </summary>
        private readonly Dictionary<DateTime, decimal> _dailyReturns = new Dictionary<DateTime, decimal>();

        //private decimal _arithmeticMonthlyMean;

        #endregion

        #region Constructor

        public CalculationContext(PriceHistoryCollection priceHistory)
        {
            _priceHistory = priceHistory;
            _handler = new CalculationHandler();
        }

        #endregion

        public ITradingRecord LastRecord => _priceHistory.LastItem;

        public ITradingRecord FirstRecord => _priceHistory.FirstItem;


        public decimal GetAbsoluteReturn(DateTime from, DateTime? to = null, CaclulationOption? option = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem)
        {
            var opt = option ?? CaclulationOption.Adjusted;
            var isLast = to == null ? true : false;
            return _handler.CalcAbsoluteReturn(_priceHistory.Get(from, priceHistoryOption), isLast ? _priceHistory.LastItem : _priceHistory.Get(to.Value, priceHistoryOption), opt);
        }

        public decimal GetDailyReturn(ITradingRecord from, ITradingRecord to)
        {
            return _handler.CalcAbsoluteReturn(from, to);
        }


        public decimal GetAverageReturn(DateTime from, DateTime? to = null, CaclulationOption? option = null)
        {
            var opt = option ?? CaclulationOption.Adjusted;
            var isLast = to == null;
            return _handler.CalcAverageReturn(_priceHistory.Get(from), isLast
                ? _priceHistory.LastItem
                : _priceHistory.Get(to.Value), opt);
        }

        public decimal GetAverageReturnMonthly(DateTime from, DateTime? to = null, CaclulationOption? option = null)
        {
            var opt = option ?? CaclulationOption.Adjusted;
            var isLast = to == null ? true : false;
            if (isLast)
                return _handler.CalcAverageReturnMonthly(_priceHistory.Get(from), _priceHistory.LastItem, opt);

            return _handler.CalcAverageReturnMonthly(_priceHistory.Get(from), _priceHistory.Get(to.Value), opt);
        }

        public decimal GetMaximumDrawdown(DateTime? from = null, DateTime? to = null, CaclulationOption? option = null)
        {
            var start = from ?? FirstRecord.Asof;
            var end = to ?? LastRecord.Asof;
            var opt = option ?? CaclulationOption.Adjusted;
            return _handler.CalcMaxDrawdown(_priceHistory.Range(start, end), opt);
        }

        public DrawdownItem GetMaximumDrawdownItem(DateTime? from = null, DateTime? to = null, CaclulationOption? option = null)
        {
            var start = from ?? FirstRecord.Asof;
            var end = to ?? LastRecord.Asof;
            var opt = option ?? CaclulationOption.Adjusted;
            return _handler.CalcMaxDrawdownItem(_priceHistory.Range(start, end), opt);
        }

        internal void AddDailyReturn(ITradingRecord from, ITradingRecord to)
        {
            if (!_dailyReturns.ContainsKey(to.Asof))
                _dailyReturns.Add(to.Asof, _handler.CalcAbsoluteReturn(from, to));
        }

        public bool ScanRange(DateTime backtestDateTime, DateTime startDateInput)
        {
            return _handler.ScanRange(_priceHistory.Range(backtestDateTime, startDateInput));
        }

        public bool ScanRangeNoLow(DateTime backtestDateTime, DateTime startDateInput)
        {
            var vola = _priceHistory.Calc.GetVolatilityMonthly(backtestDateTime, startDateInput);
            return _handler.ScanRangeNoLow(_priceHistory.Range(backtestDateTime, startDateInput), vola);
        }

        public decimal GetVolatilityMonthly(DateTime? from, DateTime? to = null, CaclulationOption? opt = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem)
        {
            var start = from ?? FirstRecord.Asof;
            var end = to ?? LastRecord.Asof;

            return _handler.CalcVolatility(GetAverageReturn(start), EnumMonthlyReturns(), opt ?? CaclulationOption.Adjusted);
        }

        public IEnumerable<decimal> EnumDailyReturns()
        {
            foreach (var item in _dailyReturns.Values)
                yield return item;
        }

        public IEnumerable<decimal> EnumMonthlyReturns()
        {
            //damit kann gleich nach MOnaten gruppiert werden mit dem Key "{MMYY}"
            foreach (var grp in _dailyReturns.GroupBy(x => new { x.Key.Month, x.Key.Year }))
                yield return grp.Sum(y => y.Value);
        }

        public IEnumerable<Tuple<DateTime, decimal>> EnumDailyReturnsTuple()
        {
            foreach (var item in _dailyReturns)
                yield return Tuple.Create(item.Key, item.Value);

        }

        public int MovingDays => _priceHistory.MovingDays;

        public void CalcArithmeticMean(ITradingRecord item, int count)
        {
            if (count == 1)
                _arithmeticMean = item.AdjustedPrice;
            else
                _arithmeticMean = (item.AdjustedPrice + _arithmeticMean) / count;
        }

        public void CalcArithmeticMeanDailyReturns()
        {
            _arithmeticMeanDailyReturns = _dailyReturns.Values.Sum() / _priceHistory.Count;
        }


        private readonly LowMetaInfoCollection _lowMetaInfos = new LowMetaInfoCollection();


        public class LowMetaInfoCollection : Dictionary<DateTime, LowMetaInfo>
        {
            private const int MAX_TRIES = 15;
            public bool TryGetLastItem(DateTime asof, out LowMetaInfo lastMetaInfo)
            {
                if (Count == 0)
                {
                    lastMetaInfo = null;
                    return false;
                }

                var idx = 0;

                while (idx < MAX_TRIES)
                {
                    if (TryGetValue(asof.AddDays(-idx), out var lastInfo))
                    {
                        var currentlastMetaInfo = lastInfo;
                        if (currentlastMetaInfo != null)
                        {
                            lastMetaInfo = currentlastMetaInfo;
                            return true;
                        }
                    }

                    if (lastInfo == null)
                        idx++;
                }

                lastMetaInfo = null;
                return false;
            }
         
        }

        public void CalcMovingLows(ITradingRecord item, int count)
        {
            //brauche erst rechnen ab dem Moment wo sich ein erstes Fenster ausgeht
            if (count < _priceHistory.MovingDays)
                return;

            ITradingRecord low = null;
            ITradingRecord first = null;
            ITradingRecord last = null;
            var records = new List<ITradingRecord>();

            //hol mir das letzt Item
            if (_lowMetaInfos.TryGetLastItem(item.Asof.AddDays(-1), out var lastLowMetaInfo))
            {
                //das Item von vor 150 Tagen
                var newFirst = _priceHistory.Get(item.Asof.AddDays(-_priceHistory.MovingDays));
                lastLowMetaInfo.UpdatePeriodeRecords(item);

                //wenn der aktuelle Preis höher ist als der vorherige kann es kein neues low geben
                if (item.AdjustedPrice > lastLowMetaInfo.Last.AdjustedPrice)
                {
                    //merke mir das item mit hasNewlow=false
                    _lowMetaInfos.Add(item.Asof, new LowMetaInfo(newFirst, lastLowMetaInfo.Low, item, lastLowMetaInfo, false));
                    return;
                }

                //wenn das letze Low tiefer liegt, und das datum des Lows noch in der Range ist brauche ich die 150 Tage nur um eines weiterschieben
                if (lastLowMetaInfo.Low.AdjustedPrice < item.AdjustedPrice && lastLowMetaInfo.Low.Asof >= newFirst.Asof)
                {
                    //merke mir das item mit hasNewlow=false
                    _lowMetaInfos.Add(item.Asof, new LowMetaInfo(newFirst, lastLowMetaInfo.Low, item, lastLowMetaInfo, false));
                    return;
                }
            }

            //neu berechnen
            foreach (var record in _priceHistory.Range(item.Asof.AddDays(-_priceHistory.MovingDays), item.Asof))
            {
                records.Add(record);

                if (low == null)
                {
                    //merke mir hier den ersten
                    low = record;
                    first = low;
                }

                //dann gibt es ein neues Low
                if (record.AdjustedPrice < low.AdjustedPrice)
                    low = record;
                //merke mir hier immer den letzten Record
                last = record;

            }
            //wenn lastLowMetaInfo == null bin ich beim ersten Record
            _lowMetaInfos.Add(item.Asof, lastLowMetaInfo != null
                    ? new LowMetaInfo(first, low, last, lastLowMetaInfo, true)
                    : new LowMetaInfo(first, low, last, records));
        }

        public bool TryGetLastLowItem(DateTime currentDate, out LowMetaInfo info)
        {
            return _lowMetaInfos.TryGetLastItem(currentDate, out info);
        }

        public IEnumerable<Tuple<DateTime, LowMetaInfo>> EnumLows()
        {
            foreach (var kvp in _lowMetaInfos)
            {
                yield return new Tuple<DateTime, LowMetaInfo>(kvp.Key, kvp.Value);
            }
        }
    }
}