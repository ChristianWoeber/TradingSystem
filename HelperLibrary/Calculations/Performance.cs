using HelperLibrary.Collections;
using HelperLibrary.Database.Interfaces;
using HelperLibrary.Database.Models;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HelperLibrary.Extensions;

namespace HelperLibrary.Calculations
{

    /// <summary>
    /// Der Calculation Context, wird nur einmal pro Price History Collection erstellt wird
    /// </summary>
    public class CalculationContext
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

        private decimal _arithmeticMonthlyMean;

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


    public class CalculationHandler
    {
        private ICalculation _calc;

        public CalculationHandler()
        {

        }

        public CalculationHandler(ICalculation calc)
        {
            _calc = calc;
        }

        public decimal CalcAverageReturn(ITradingRecord from, ITradingRecord to, CaclulationOption option)
        {
            // Wenn einer der beiden Werte null ist gebe ich -1 zurück
            if (from == null || to == null)
                return decimal.MinusOne;

            var years = GetYears(from.Asof, to.Asof);
            if (years < 1)
                return CalcAbsoluteReturn(from, to, option);

            return (decimal)Math.Pow(1 + (double)CalcAbsoluteReturn(from, to, option), 1 / GetYears(from.Asof, to.Asof)) - 1;
        }

        public decimal CalcAverageReturnMonthly(ITradingRecord from, ITradingRecord to, CaclulationOption option)
        {
            // Wenn einer der beiden Werte null ist gebe ich -1 zurück
            if (from == null || to == null)
                return decimal.MinusOne;

            var years = GetYears(from.Asof, to.Asof);
            if (years < 1)
                return CalcAbsoluteReturn(from, to, option);

            return (decimal)Math.Pow(1 + (double)CalcAbsoluteReturn(from, to, option), 1 / GetMonths(from.Asof, to.Asof)) - 1;
        }

        private double GetYears(DateTime from, DateTime to)
        {
            return (double)(to - from).Days / 365;
        }
        private double GetMonths(DateTime from, DateTime to)
        {
            return (double)(to - from).Days / 30;
        }

        public decimal CalcAbsoluteReturn(ITradingRecord from, ITradingRecord to, CaclulationOption option = CaclulationOption.Adjusted)
        {
            // Wenn einer der beiden Werte null ist gebe ich -1 zurück
            if (from == null || to == null)
                return decimal.MinusOne;

            try
            {
                switch (option)
                {
                    case CaclulationOption.Adjusted:
                        return to.AdjustedPrice / from.AdjustedPrice - 1;
                    case CaclulationOption.NonAdjusted:
                        return to.Price / from.Price - 1;
                    default:
                        return decimal.MinusOne;
                }
            }
            catch (DivideByZeroException ex)
            {
                Trace.TraceError($"Fehler aufgetreten bei: Id:{from.SecurityId}| Price:{from.Price}| Datum:{from.Asof}| AdjustedPrice:{from.AdjustedPrice}");
                return 0;
            }

        }

        public bool ScanRange(IEnumerable<ITradingRecord> priceHistoryRange, CaclulationOption opt = CaclulationOption.Adjusted)
        {
            var range = priceHistoryRange.ToList();
            var countDays = range.Count;
            var positiveCount = 0;

            //start with second day
            for (int i = 1; i < countDays; i++)
            {
                var dailyReturn = CalcAbsoluteReturn(range[i - 1], range[i]);
                if (dailyReturn > 0)
                    positiveCount++;
            }

            //at least more than 50% of the days neeed to be positive

            return ((double)positiveCount / (double)countDays) >= 0.4;
        }

        public bool ScanRangeNoLow(IEnumerable<ITradingRecord> priceHistoryRange, decimal vola, CaclulationOption opt = CaclulationOption.Adjusted)
        {
            var range = priceHistoryRange.ToList();

            //first adjusted price is the start low
            var low = range[0].AdjustedPrice;


            //start with second day
            for (int i = 1; i < range.Count; i++)
            {
                //if the low is lower then the vola we return false
                if (range[i].AdjustedPrice < low * vola)
                    return false;

            }
            return true;
        }



        public decimal CalcMaxDrawdown(IEnumerable<ITradingRecord> priceHistoryRange, CaclulationOption opt)
        {
            var drawdown = new MaxDrawdown(priceHistoryRange, opt, CalcAbsoluteReturn);
            var item = drawdown.Calculate();
            return item?.Drawdown ?? decimal.Zero;
        }

        public DrawdownItem CalcMaxDrawdownItem(IEnumerable<ITradingRecord> priceHistoryRange, CaclulationOption opt)
        {
            var drawdown = new MaxDrawdown(priceHistoryRange, opt, CalcAbsoluteReturn);
            var item = drawdown.Calculate();
            return item;
        }

        public decimal CalcVolatility(PriceHistoryCollection priceHistory, CaclulationOption opt)
        {
            var averageReturn = CalcAverageReturn(priceHistory.FirstItem, priceHistory.LastItem, opt);
            var items = priceHistory.EnumMonthlyUltimoItems().ToList();

            if (items.Count <= 5)
                return -1;

            double variance = 0;

            for (int i = 1; i < items.Count; i++)
            {
                var monthlyReturn = CalcAbsoluteReturn(items[i - 1], items[i], opt);
                variance += Math.Pow((double)averageReturn - (double)monthlyReturn, 2);

            }

            return (decimal)Math.Sqrt(variance / (items.Count - 1));
        }

        public decimal CalcVolatility(decimal averageReturn, IEnumerable<decimal> returns, CaclulationOption opt)
        {
            var variance = 0d;
            var count = 0;

            foreach (var ret in returns)
            {
                variance += Math.Pow((double)averageReturn - (double)ret, 2);
                count++;
            }

            return (decimal)Math.Sqrt(variance / count);
        }
    }
}
