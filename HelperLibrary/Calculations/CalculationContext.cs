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
    public class CalculationContext : ICalculationContext
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

        public bool TryGetDailyReturn(DateTime asof, out decimal dailyReturn)
        {
            if (_dailyReturns.Count == 0)
            {
                dailyReturn = decimal.MinusOne;
                return false;
            }

            var idx = 0;

            while (idx < MAX_TRIES)
            {
                if (_dailyReturns.TryGetValue(asof.AddDays(-idx), out var lastDailyReturn))
                {
                    dailyReturn = lastDailyReturn;
                    return true;
                }

                idx++;
            }
            dailyReturn = decimal.MinusOne;
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
            if (!_dailyReturns.ContainsKey(to.Asof))
                _dailyReturns.Add(to.Asof, _handler.CalcAbsoluteReturn(from, to));
        }

        private readonly List<Tuple<DateTime, decimal>> _monthlyReturns = new List<Tuple<DateTime, decimal>>();
        internal ITradingRecord LastUltimoRecord;

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


        internal class MovingVolatilityCollection : LastItemDictionaryBase<MovingVolaMetaInfo>
        {
        }

        public class LowMetaInfoCollection : LastItemDictionaryBase<LowMetaInfo>
        {
        }

        public abstract class LastItemDictionaryBase<TValue> : Dictionary<DateTime, TValue> where TValue : class
        {
            private const int MAX_TRIES = 30;

            private KeyValuePair<DateTime, TValue> _lastKeyValuePair;

            /// <summary>
            /// überschreibe hier die add methode
            /// und merke mir den zuletzt eingefügten wert
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            public new void Add(DateTime key, TValue value)
            {
                _lastKeyValuePair = new KeyValuePair<DateTime, TValue>(key, value);
                base.Add(key, value);
            }

            public bool TryGetLastItem(DateTime key, out TValue lastMetaInfo)
            {
                lastMetaInfo = null;
                if (Count == 0)
                {
                    return false;
                }

                if (_lastKeyValuePair.Key <= DateTime.MinValue)
                    return false;

                //Dadurch erspare ich mir beim einfüllen der Daten unnötige Rekursionen
                if (_lastKeyValuePair.Key <= key)
                {
                    lastMetaInfo = _lastKeyValuePair.Value;
                    return true;
                }

                //Normaler Modus (HINT: eventuell auch hier binarySearch verwenden?)
                var idx = 0;

                while (idx < MAX_TRIES)
                {
                    var newDate = key.AddDays(-idx);
                    if (TryGetValue(newDate, out var lastInfo))
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

        public bool TryGetLastVolatility(DateTime asof, out decimal volatility)
        {
            volatility = decimal.MinusOne;
            if (!_movingVolaMetaInfos.TryGetLastItem(asof, out var metaInfo))
                return false;

            volatility = metaInfo.DailyVolatility;
            return true;
        }

        private readonly MovingVolatilityCollection _movingVolaMetaInfos = new MovingVolatilityCollection();

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

                    _movingVolaMetaInfos.Add(item.Asof, MovingVolaMetaInfo.Create(lastMetaInfo, item, firstDailyReturn, lastDailyReturn, daysCount));
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


        public void CalcMovingLows(ITradingRecord item, int count)
        {
            //brauche erst rechnen ab dem Moment wo sich ein erstes Fenster ausgeht
            if (count < _priceHistory.Settings.MovingAverageLengthInDays)
                return;

            try
            {

                ITradingRecord low = null;
                ITradingRecord first = null;
                ITradingRecord last = null;
                var records = new List<ITradingRecord>();

                //hol mir das letzt Item
                if (_lowMetaInfos.TryGetLastItem(item.Asof.AddDays(-1), out var lastLowMetaInfo))
                {
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public bool TryGetLastLowInfo(DateTime currentDate, out LowMetaInfo info)
        {
            return _lowMetaInfos.TryGetLastItem(currentDate, out info);
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

    public class MovingVolaMetaInfo
    {
        private readonly IPriceHistoryCollectionSettings _settings;
        private readonly int _count;

        public MovingVolaMetaInfo(decimal averageReturn, decimal variance, IPriceHistoryCollectionSettings settings, int count)
        {
            _settings = settings;
            _count = count;
            AverageReturn = averageReturn;
            Variance = variance;
            //Wurzel aus varianz/ N-1 und auf 250 Tage bringen
            DailyVolatility = (decimal)(Math.Sqrt((double)variance / (count - 1)) * Math.Sqrt(settings.MovingDaysVolatility));
        }
        /// <summary>
        /// die tägliche Volatilität
        /// </summary>
        public decimal DailyVolatility { get; }

        /// <summary>
        /// das arithmetrische MIttel der daily Returns
        /// </summary>
        public decimal AverageReturn { get; }

        /// <summary>
        /// die Varianz => Achtung ist schon durch N-1 bereiningt
        /// </summary>
        public decimal Variance { get; }


        public static MovingVolaMetaInfo Create(MovingVolaMetaInfo lastMetaInfo, ITradingRecord item, decimal firstDailyReturn, decimal lastDailyReturn, int count)
        {
            //ändere hier nur den letzen und ersten Value
            var currentAverage = lastMetaInfo.AverageReturn;
            currentAverage += lastDailyReturn * 1 / (count - 1);
            currentAverage -= firstDailyReturn * 1 / (count - 1);

            //auch bei der Varianz
            var currentVariance = lastMetaInfo.Variance;
            currentVariance += (decimal)Math.Pow(((double)currentAverage - (double)lastDailyReturn), 2);
            currentVariance -= (decimal)Math.Pow(((double)lastMetaInfo.AverageReturn - (double)firstDailyReturn), 2);

            //Trace.TraceInformation($"aktuelle Varianz: {currentVariance:N6}, aktueller Average: {currentAverage:N6}, aktuelles Datum {item.Asof}");
            //gebe dann die aktualisierte Info zurück
            return new MovingVolaMetaInfo(currentAverage, currentVariance, lastMetaInfo._settings, count);
        }
    }
}