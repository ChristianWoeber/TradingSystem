using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Database.Interfaces;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Calculations
{
    internal class MaxDrawdown
    {
        private CalculationOption _opt;
        private IEnumerable<ITradingRecord> _priceHistoryRange;
        private Func<ITradingRecord, ITradingRecord, CalculationOption, decimal> _calcAbsoluteReturnFunc;

        public MaxDrawdown(IEnumerable<ITradingRecord> priceHistoryRange, CalculationOption opt)
        {
            _priceHistoryRange = priceHistoryRange;
            _opt = opt;
        }

        public MaxDrawdown(IEnumerable<ITradingRecord> priceHistoryRange, CalculationOption opt, Func<ITradingRecord, ITradingRecord, CalculationOption, decimal> calcAbsoluteReturn) :
            this(priceHistoryRange, opt)
        {
            _calcAbsoluteReturnFunc = calcAbsoluteReturn;
        }

        public DrawdownItem Calculate()
        {
            switch (_opt)
            {
                case CalculationOption.Adjusted:
                    return CalculateMaxDrawDownAdjusted();
                case CalculationOption.NonAdjusted:
                    return CalculateMaxDrawDownNonAdjusted();
                default:
                    return null; ;
            }
        }

        private DrawdownItem CalculateMaxDrawDownNonAdjusted()
        {
            var drawdowns = new List<DrawdownItem>();
            ITradingRecord high = null;
            ITradingRecord low = null;
            var isFirst = true;

            foreach (var item in _priceHistoryRange.OrderBy(x=>x.Asof))
            {
                if (isFirst)
                {
                    high = item;
                    low = item;
                    isFirst = false;
                    continue;
                }

                // is current price higher then last High
                if (item.Price > high.Price)
                {
                    //we have a new High
                    high = item;

                    // everytime we reach a new high we have to rest the low
                    low = item;

                }
                // if the current price is lower then the lat low
                else if (item.Price < low.Price)
                {
                    low = item;
                    drawdowns.Add(new DrawdownItem { End = low, Start = high, Drawdown = _calcAbsoluteReturnFunc(high, low, _opt) });

                }
            }
            var orderd = drawdowns.OrderBy(x => x.Drawdown);
            return orderd.FirstOrDefault();
        }

        private DrawdownItem CalculateMaxDrawDownAdjusted()
        {
            var drawdowns = new List<DrawdownItem>();
            ITradingRecord high = null;
            ITradingRecord low = null;
            var isFirst = true;

            foreach (var item in _priceHistoryRange)
            {
                if (isFirst)
                {
                    high = item;
                    low = item;
                    isFirst = false;
                    continue;
                }
                // is current price higher then last High
                if (item.AdjustedPrice > high.AdjustedPrice)
                {
                    //we have a new High
                    high = item;

                    // everytime we reach a new high we have to rest the low
                    low = item;
                   
                }
                // if the current price is lower then the lat low
                else if (item.AdjustedPrice < low.AdjustedPrice)
                {
                    low = item;
                    drawdowns.Add(new DrawdownItem { End = low, Start = high, Drawdown = _calcAbsoluteReturnFunc(high, low, _opt) });
                    
                }
            }
            var orderd = drawdowns.OrderBy(x => x.Drawdown);
            return orderd.FirstOrDefault();
        }
    }
}