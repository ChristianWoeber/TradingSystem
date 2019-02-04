using System;
using System.Collections.Generic;
using Trading.DataStructures.Enums;

namespace Trading.DataStructures.Interfaces
{
    public interface ICalculationContext
    {
        decimal GetAbsoluteReturn(DateTime from, DateTime? to = null, CaclulationOption? option = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem);
        bool TryGetDailyReturn(DateTime asof, out decimal dailyReturn);
        decimal GetAverageReturn(DateTime from, DateTime? to = null, CaclulationOption? option = null);
        decimal GetAverageReturnMonthly(DateTime from, DateTime? to = null, CaclulationOption? option = null);
        decimal GetMaximumDrawdown(DateTime? from = null, DateTime? to = null, CaclulationOption? option = null);
        decimal GetVolatilityMonthly(DateTime? from, DateTime? to = null, CaclulationOption? opt = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem);
        IEnumerable<decimal> EnumDailyReturns();
        IEnumerable<decimal> EnumMonthlyReturns();
        IEnumerable<Tuple<DateTime, decimal>> EnumDailyReturnsTuple();
        bool TryGetLastVolatility(DateTime asof, out decimal volatility);
        bool DateIsNewLow(DateTime currentDate);


    }
}