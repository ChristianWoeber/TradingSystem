using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using HelperLibrary.Collections;
using HelperLibrary.Database.Models;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;


namespace HelperLibrary.Calculations
{
    /// <summary>
    /// Der Calculation Context, wird nur einmal pro Price History Collection erstellt wird
    /// </summary>
    public partial class CalculationContext : ICalculationContext
    {
        #region Private Member

        private readonly PriceHistoryCollection _priceHistory;
        private readonly CalculationHandler _handler;
        private decimal _arithmeticMean;
        private decimal _arithmeticMeanDailyReturns;
        private const int MAX_TRIES = 15;
        #endregion

        #region Collections

        /// <summary>
        /// the Daily Returns
        /// </summary>
        private readonly DailyReturnCollection _dailyReturns = new DailyReturnCollection();
        private readonly LowMetaInfoCollection _lowMetaInfos = new LowMetaInfoCollection();
        private readonly MovingVolatilityCollection _movingVolaMetaInfos = new MovingVolatilityCollection();
        private readonly AbsoluteLossAndGainCollection _absoluteLossAndGainInfos = new AbsoluteLossAndGainCollection();

        private readonly List<Tuple<DateTime, decimal>> _monthlyReturns = new List<Tuple<DateTime, decimal>>();

        /// <summary>
        /// die Collection für die Moving Vola
        /// </summary>
        public class MovingVolatilityCollection : LastItemDictionaryBase<MovingVolaMetaInfo> { }

        /// <summary>
        /// Die Collection für die LowMetaInfos
        /// </summary>
        public class LowMetaInfoCollection : LastItemDictionaryBase<LowMetaInfo> { }


        /// <summary>
        /// Die Collection für die AbsoluteLossesAndGainsMetaInfo
        /// </summary>
        public class AbsoluteLossAndGainCollection : LastItemDictionaryBase<AbsoluteLossesAndGainsMetaInfo> { }

        /// <summary>
        /// die Collection für die Dailly Returns
        /// </summary>
        public class DailyReturnCollection : LastItemDictionaryBase<DailyReturnMetaInfo>
        {

        }



        #endregion

        #region Constructor

        public CalculationContext(PriceHistoryCollection priceHistory)
        {
            _priceHistory = priceHistory;
            _handler = new CalculationHandler();
        }

        #endregion

        internal ITradingRecord LastUltimoRecord;

        public ITradingRecord LastRecord => _priceHistory.LastItem;

        public ITradingRecord FirstRecord => _priceHistory.FirstItem;


        public decimal GetAbsoluteReturn(DateTime from, DateTime? to = null, CaclulationOption? option = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem)
        {
            var opt = option ?? CaclulationOption.Adjusted;
            var isLast = to == null ? true : false;
            return _handler.CalcAbsoluteReturn(_priceHistory.Get(from, priceHistoryOption), isLast ? _priceHistory.LastItem : _priceHistory.Get(to.Value, priceHistoryOption), opt);
        }

        public bool TryGetDailyReturn(DateTime asof, out IDailyReturnMetaInfo dailyReturnMetaInfo)
        {
            dailyReturnMetaInfo = null;
            if (_dailyReturns.TryGetLastItem(asof, out var info))
            {
                dailyReturnMetaInfo = info;
                return true;
            }

            return false;
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
            if (_dailyReturns.LastItem == null || _dailyReturns.LastItem?.Key < to.Asof)
            {
                var metaInfo = new DailyReturnMetaInfo(from, to, _handler.CalcAbsoluteReturn(from, to));
                _dailyReturns.Add(to.Asof, metaInfo);
            }
        }


        public void AddMonthlyUltimoReturn(ITradingRecord ultimoRecord)
        {
            _monthlyReturns.Add(new Tuple<DateTime, decimal>(ultimoRecord.Asof, _handler.CalcAbsoluteReturn(LastUltimoRecord, ultimoRecord)));
            LastUltimoRecord = ultimoRecord;
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

            return _handler.CalcVolatility(EnumMonthlyReturns(), opt ?? CaclulationOption.Adjusted);
        }

        public IEnumerable<decimal> EnumDailyReturns()
        {
            foreach (var item in _dailyReturns)
                yield return item.Value.AbsoluteReturn;
        }

        public IEnumerable<IDailyReturnMetaInfo> EnumDailyReturnMetaInfos()
        {
            foreach (var item in _dailyReturns)
                yield return item.Value;
        }

        public IEnumerable<decimal> EnumMonthlyReturns()
        {
            //damit kann gleich nach MOnaten gruppiert werden mit dem Key "{MMYY}"
            foreach (var grp in _dailyReturns.GroupBy(x => new { x.Key.Month, x.Key.Year }))
                yield return grp.Sum(y => y.Value.AbsoluteReturn);
        }

        public IEnumerable<Tuple<DateTime, decimal>> EnumDailyReturnsTuple()
        {
            foreach (var item in _dailyReturns)
                yield return Tuple.Create(item.Key, item.Value.AbsoluteReturn);
        }

        public void CalcArithmeticMean(ITradingRecord item, int count)
        {
            if (count == 1)
                _arithmeticMean = item.AdjustedPrice;
            else
                _arithmeticMean = (item.AdjustedPrice + _arithmeticMean) / count;
        }


        public bool TryGetLastVolatility(DateTime asof, out decimal? volatility)
        {
            volatility = null;
            if (!_movingVolaMetaInfos.TryGetLastItem(asof, out var metaInfo))
                return false;

            volatility = metaInfo.DailyVolatility;
            return true;
        }


        public void CalcMovingVola(ITradingRecord item, int count)
        {
            try
            {
                if (_movingVolaMetaInfos.Count > 0)
                {
                    if (!_movingVolaMetaInfos.TryGetLastItem(item.Asof, out var lastMetaInfo))
                        throw new ArgumentException("Achtung keine MetaInfo gefunden bei" + _priceHistory.Settings?.Name);

                    var firstItem = _priceHistory.Get(_movingVolaMetaInfos.Count);
                    var daysCount = _priceHistory.Count - _movingVolaMetaInfos.Count;

                    //hole mir hier den letzten und den aktuellen Daily Return und aktualisere damit die Berechnung
                    //sonst werfe ich eine exception
                    if (!TryGetDailyReturn(item.Asof, out var lastDailyReturn) || !TryGetDailyReturn(firstItem.Asof, out var firstDailyReturn))
                        throw new ArgumentException("Achtung es konnten keine DailyReturns gefunden werden");

                    _movingVolaMetaInfos.Add(item.Asof, MovingVolaMetaInfo.Create(lastMetaInfo, item, firstDailyReturn.AbsoluteReturn, lastDailyReturn.AbsoluteReturn, daysCount));
                    return;
                }

                // --------- hier komme ich nur initial rein und durchlaufe die records 2 mal ----------- //

                var variance = 0d;
                //einmal alle Returns druchlaufen und den Average berechnen
                var averageReturn = EnumDailyReturns().Average();

                //danach nich einmal durchlaufen und die varianz rechnen
                foreach (var dailyReturn in EnumDailyReturns())
                {
                    variance += Math.Pow((double)dailyReturn - (double)averageReturn, 2);
                }

                //dann brauche ich den eigentlichen Count nicht injecten
                _movingVolaMetaInfos.Add(item.Asof, new MovingVolaMetaInfo(averageReturn, (decimal)variance, _priceHistory.Settings, count));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void CalcAbsoluteLossesAndGains(ITradingRecord item, int count)
        {
            try
            {
                if (!TryGetLastAbsoluteLossAndGain(item.Asof, out var absoluteLossesAndGainsMetaInfo))
                {
                    var metaInfo = GetLastAbsoluteLossAndGainMetaInfo();
                    _absoluteLossAndGainInfos.Add(item.Asof, metaInfo);
                }
                else
                {
                    if (!TryGetDailyReturn(item.Asof, out var dailyReturn))
                        throw new ArgumentException($"Achtung zu diesem zeitpunkt konnte kein daily Return gefunden werden {item.Asof}");

                    var metaInfo = new AbsoluteLossesAndGainsMetaInfo(absoluteLossesAndGainsMetaInfo);
                    metaInfo.Update(dailyReturn);
                    _absoluteLossAndGainInfos.Add(item.Asof, metaInfo);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        public bool TryGetLastAbsoluteLossAndGain(DateTime itemAsof, out IAbsoluteLossesAndGainsMetaInfo info)
        {
            info = _absoluteLossAndGainInfos.Get(itemAsof, BinarySearchOption.GetLastIfNotFound)?.Value;
            return info != null;
        }

        private AbsoluteLossesAndGainsMetaInfo GetLastAbsoluteLossAndGainMetaInfo()
        {
            decimal absoluteLoss = 0;
            decimal absoluteGain = 0;

            foreach (var dailyReturn in _dailyReturns)
            {
                if (dailyReturn.Value.AbsoluteReturn > 0)
                    absoluteGain += dailyReturn.Value.AbsoluteReturn;
                else
                {
                    absoluteLoss += dailyReturn.Value.AbsoluteReturn;
                }
            }

            return new AbsoluteLossesAndGainsMetaInfo(absoluteLoss, absoluteGain, _priceHistory.Settings, _dailyReturns.Select(x => (IDailyReturnMetaInfo)x.Value).ToList());
        }

        public void CalcMovingLows(ITradingRecord item, int count)
        {
            //brauche erst rechnen ab dem Moment wo sich ein erstes Fenster ausgeht
            if (count < _priceHistory.Settings.MovingAverageLengthInDays)
                return;

            try
            {
                ITradingRecord high = null;
                ITradingRecord low = null;
                ITradingRecord first = null;
                ITradingRecord last = null;
                var records = new List<ITradingRecord>();

                //hol mir das letzte Item
                if (_lowMetaInfos.TryGetLastItem(item.Asof.AddDays(-1), out var lastLowMetaInfo))
                {

                    //TODO: fix
                    //das High aktualisieren
                    if (item.AdjustedPrice > lastLowMetaInfo.High?.AdjustedPrice)
                        lastLowMetaInfo.UpdateHigh(item);

                    //das Item von vor 150 Tagen
                    var newFirst = _priceHistory.Get(item.Asof.AddDays(-_priceHistory.Settings.MovingAverageLengthInDays));
                    lastLowMetaInfo.UpdatePeriodeRecords(item);

                    //wenn der aktuelle Preis höher ist als der vorherige kann es kein neues low geben
                    if (item.AdjustedPrice > lastLowMetaInfo.Last.AdjustedPrice)
                    {
                        //merke mir das item mit hasNewlow=false
                        _lowMetaInfos.Add(item.Asof, new LowMetaInfo(newFirst, lastLowMetaInfo.Low, item, lastLowMetaInfo, false));
                        return;
                    }

                    //wenn das letze Low tiefer liegt, und das datum des Lows noch in der Range ist brauche ich die 150 Tage nur um eines weiterschieben
                    if (lastLowMetaInfo.Low.AdjustedPrice < item.AdjustedPrice && lastLowMetaInfo.Low.Asof >= newFirst?.Asof)
                    {
                        //merke mir das item mit hasNewlow=false
                        _lowMetaInfos.Add(item.Asof, new LowMetaInfo(newFirst, lastLowMetaInfo.Low, item, lastLowMetaInfo, false));
                        return;
                    }
                }

                //neu berechnen
                foreach (var record in _priceHistory.Range(item.Asof.AddDays(-_priceHistory.Settings.MovingAverageLengthInDays), item.Asof))
                {
                    records.Add(record);

                    if (low == null)
                    {
                        //merke mir hier den ersten
                        low = new TradingRecord(record);
                        first = new TradingRecord(record);
                        high = new TradingRecord(record);
                    }

                    //dann gibt es ein neues Low
                    if (record.AdjustedPrice < low.AdjustedPrice)
                        low = new TradingRecord(record);
                    //auch das high merken
                    if (record.AdjustedPrice > high.AdjustedPrice)
                        high = new TradingRecord(record);

                    //merke mir hier immer den letzten Record
                    last = record;
                }
                //wenn lastLowMetaInfo == null bin ich beim ersten Record
                _lowMetaInfos.Add(item.Asof, lastLowMetaInfo != null
                        ? new LowMetaInfo(first, low, last, lastLowMetaInfo, true)
                        : new LowMetaInfo(first, low, last, records));

                //das high nachziehen
                _lowMetaInfos.LastItem.Value.UpdateHigh(high);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public bool TryGetLastLowInfo(DateTime currentDate, out ILowMetaInfo info)
        {
            info = null;
            if (!_lowMetaInfos.TryGetLastItem(currentDate, out var infoInternal))
                return false;
            info = infoInternal;
            return true;

        }

        public bool DateIsNewLow(DateTime currentDate)
        {
            return _lowMetaInfos.TryGetLastItem(currentDate, out var info) && info.HasNewLow;
        }


        public bool TryGetLastVolatilityInfo(DateTime currentDate, out MovingVolaMetaInfo vola)
        {
            return _movingVolaMetaInfos.TryGetLastItem(currentDate, out vola);
        }

        public IEnumerable<Tuple<DateTime, LowMetaInfo>> EnumLows()
        {
            foreach (var kvp in _lowMetaInfos)
            {
                yield return new Tuple<DateTime, LowMetaInfo>(kvp.Key, kvp.Value);
            }
        }
    }

    public class DailyReturnMetaInfo : IDailyReturnMetaInfo
    {
        public DailyReturnMetaInfo(ITradingRecord fromRecord, ITradingRecord toRecord, decimal absoluteReturn)
        {
            FromRecord = fromRecord;
            ToRecord = toRecord;
            AbsoluteReturn = absoluteReturn;
        }

        /// <summary>
        /// Der komplette Record from
        /// </summary>
        public ITradingRecord FromRecord { get; }

        /// <summary>
        /// Der komplette Record to
        /// </summary>
        public ITradingRecord ToRecord { get; }

        /// <summary>
        /// der Return 
        /// </summary>
        public decimal AbsoluteReturn { get; }

        /// <summary>Gibt eine Zeichenfolge zurück, die das aktuelle Objekt darstellt.</summary>
        /// <returns>Eine Zeichenfolge, die das aktuelle Objekt darstellt.</returns>
        public override string ToString()
        {
            return $"{FromRecord.Asof.ToShortDateString()}_{AbsoluteReturn}";
        }
    }
}