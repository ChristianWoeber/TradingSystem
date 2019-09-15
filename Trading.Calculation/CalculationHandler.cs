using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    public class CalculationHandler
    {
        //private ICalculation _calc;

        public CalculationHandler()
        {

        }

        //public CalculationHandler(ICalculation calc)
        //{
        //    _calc = calc;
        //}

        public decimal CalcAverageReturn(ITradingRecord from, ITradingRecord to, CalculationOption option = CalculationOption.Adjusted)
        {
            // Wenn einer der beiden Werte null ist gebe ich -1 zurück
            if (from == null || to == null)
                return decimal.MinusOne;

            var years = GetYears(from.Asof, to.Asof);
            if (years < 1)
                return CalcAbsoluteReturn(from, to, option);

            return (decimal)Math.Pow(1 + (double)CalcAbsoluteReturn(from, to, option), 1 / GetYears(from.Asof, to.Asof)) - 1;
        }

        public decimal CalcAverageReturnMonthly(ITradingRecord from, ITradingRecord to, CalculationOption option)
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

        public decimal CalcAbsoluteReturn(ITradingRecord from, ITradingRecord to, CalculationOption option = CalculationOption.Adjusted)
        {
            // Wenn einer der beiden Werte null ist gebe ich -1 zurück
            if (from == null || to == null)
                return decimal.MinusOne;

            try
            {
                switch (option)
                {
                    case CalculationOption.Adjusted:
                        return to.AdjustedPrice / from.AdjustedPrice - 1;
                    case CalculationOption.NonAdjusted:
                        return to.Price / from.Price - 1;
                    default:
                        return decimal.MinusOne;
                }
            }
            catch (DivideByZeroException)
            {
                Trace.TraceError($"Fehler aufgetreten bei: Id:{from.SecurityId}| Price:{from.Price}| Datum:{from.Asof}| AdjustedPrice:{from.AdjustedPrice}");
                return 0;
            }

        }

        public bool ScanRange(IEnumerable<ITradingRecord> priceHistoryRange, CalculationOption opt = CalculationOption.Adjusted)
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

        public bool ScanRangeNoLow(IEnumerable<ITradingRecord> priceHistoryRange, decimal vola, CalculationOption opt = CalculationOption.Adjusted)
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



        public decimal CalcMaxDrawdown(IEnumerable<ITradingRecord> priceHistoryRange, CalculationOption opt)
        {
            var drawdown = new MaxDrawdown(priceHistoryRange, opt, CalcAbsoluteReturn);
            var item = drawdown.Calculate();
            return item?.Drawdown ?? decimal.Zero;
        }

        public DrawdownMetaInfo CalcMaxDrawdownItem(IEnumerable<ITradingRecord> priceHistoryRange, CalculationOption opt)
        {
            var drawdown = new MaxDrawdown(priceHistoryRange, opt, CalcAbsoluteReturn);
            var item = drawdown.Calculate();
            return item;
        }

        public decimal CalcVolatility(IPriceHistoryCollection priceHistory, CalculationOption opt)
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

        public decimal CalcVolatility(IEnumerable<decimal> returns, CalculationOption opt)
        {
            var variance = 0d;
            decimal averageReturn = 0;

            var monthlyReturns = returns.ToList();
            for (var i = 0; i < monthlyReturns.Count - 1; i++)
            {
                var ret = monthlyReturns[i];
                averageReturn = ret / (i + 1);
            }

            foreach (var ret in monthlyReturns)
            {
                variance += Math.Pow((double)averageReturn - (double)ret, 2);
            }

            return (decimal)Math.Sqrt(variance / (monthlyReturns.Count - 1)) * (decimal)Math.Sqrt(20);
        }
    }
}
