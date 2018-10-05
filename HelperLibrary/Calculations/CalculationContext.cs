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
            var isLast = to == null ? true : false;
            if (isLast)
                return _handler.CalcAverageReturn(_priceHistory.Get(from), _priceHistory.LastItem, opt);

            return _handler.CalcAverageReturn(_priceHistory.Get(from), _priceHistory.Get(to.Value), opt);
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

        public void CalcArithmeticMean(ITradingRecord item)
        {
            _arithmeticMean = _dailyReturns.Values.Sum() / _dailyReturns.Count;


            //if (item.Price <= decimal.MinValue && item.AdjustedPrice <= decimal.MinValue)
            //    throw new ArgumentException($"Achtung es wurde kein gültiger Price mitgegeben - werder {nameof(item.AdjustedPrice)} noch {nameof(item.Price)}");
            //if (item.AdjustedPrice <= decimal.MinValue)
            //    _arithmeticMean += item.Price / _dailyReturns.Count;
            //else
            //    _arithmeticMean += item.AdjustedPrice / _dailyReturns.Count;
        }
    }
}