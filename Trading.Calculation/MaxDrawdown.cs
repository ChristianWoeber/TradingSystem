using System;
using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    internal class MaxDrawdown
    {
        private readonly CalculationOption _opt;
        private readonly IEnumerable<ITradingRecord> _priceHistoryRange;
        private readonly Func<ITradingRecord, ITradingRecord, CalculationOption, decimal> _calcAbsoluteReturnFunc;

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

        public DrawdownMetaInfo Calculate()
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

        private DrawdownMetaInfo CalculateMaxDrawDownNonAdjusted()
        {
            var drawdowns = new List<DrawdownMetaInfo>();
            ITradingRecord high = null;
            ITradingRecord low = null;
            var isFirst = true;

            foreach (var item in _priceHistoryRange.OrderBy(x => x.Asof))
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
                    drawdowns.Add(new DrawdownMetaInfo { End = low, Start = high, Drawdown = _calcAbsoluteReturnFunc(high, low, _opt) });

                }
            }
            var orderd = drawdowns.OrderBy(x => x.Drawdown);
            return orderd.FirstOrDefault();
        }

        private DrawdownMetaInfo CalculateMaxDrawDownAdjusted()
        {
            var drawdowns = new List<DrawdownMetaInfo>();
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
                    drawdowns.Add(new DrawdownMetaInfo { End = low, Start = high, Drawdown = _calcAbsoluteReturnFunc(high, low, _opt) });

                }
            }
            var orderd = drawdowns.OrderBy(x => x.Drawdown);
            return orderd.FirstOrDefault();
        }
    }
}