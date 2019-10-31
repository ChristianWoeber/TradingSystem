using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trading.Calculation.Collections;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    /// <summary>
    /// Der Calculation Context, wird nur einmal pro Price History Collection erstellt wird
    /// </summary>
    public partial class CalculationContext : ICalculationContext
    {
        #region Private Member

        private readonly IPriceHistoryCollection _priceHistory;
        private readonly CalculationHandler _calculationHandler;
        private decimal _arithmeticMean;

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

        public CalculationContext(IPriceHistoryCollection priceHistory)
        {
            _priceHistory = priceHistory;
            _calculationHandler = new CalculationHandler();
        }

        #endregion

        internal ITradingRecord LastUltimoRecord;

        public ITradingRecord LastRecord => _priceHistory.LastItem;

        public ITradingRecord FirstRecord => _priceHistory.FirstItem;


        public decimal GetAbsoluteReturn(DateTime from, DateTime? to = null, CalculationOption? option = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem)
        {
            var opt = option ?? CalculationOption.Adjusted;
            var isLast = to == null ? true : false;
            return _calculationHandler.CalcAbsoluteReturn(_priceHistory.Get(from, priceHistoryOption), isLast ? _priceHistory.LastItem : _priceHistory.Get(to.Value, priceHistoryOption), opt);
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


        public decimal GetAverageReturn(DateTime from, DateTime? to = null, CalculationOption? option = null)
        {
            var opt = option ?? CalculationOption.Adjusted;
            var isLast = to == null;
            return _calculationHandler.CalcAverageReturn(_priceHistory.Get(from), isLast
                ? _priceHistory.LastItem
                : _priceHistory.Get(to.Value), opt);
        }

        public decimal GetAverageReturnMonthly(DateTime from, DateTime? to = null, CalculationOption? option = null)
        {
            var opt = option ?? CalculationOption.Adjusted;
            var isLast = to == null ? true : false;
            if (isLast)
                return _calculationHandler.CalcAverageReturnMonthly(_priceHistory.Get(from), _priceHistory.LastItem, opt);

            return _calculationHandler.CalcAverageReturnMonthly(_priceHistory.Get(from), _priceHistory.Get(to.Value), opt);
        }

        public decimal GetMaximumDrawdown(DateTime? from = null, DateTime? to = null, CalculationOption? option = null)
        {
            var start = from ?? FirstRecord.Asof;
            var end = to ?? LastRecord.Asof;
            var opt = option ?? CalculationOption.Adjusted;
            return _calculationHandler.CalcMaxDrawdown(_priceHistory.Range(start, end), opt);
        }

        public DrawdownMetaInfo GetMaximumDrawdownItem(DateTime? from = null, DateTime? to = null, CalculationOption? option = null)
        {
            var start = from ?? FirstRecord.Asof;
            var end = to ?? LastRecord.Asof;
            var opt = option ?? CalculationOption.Adjusted;
            return _calculationHandler.CalcMaxDrawdownItem(_priceHistory.Range(start, end), opt);
        }

        public void AddDailyReturn(ITradingRecord from, ITradingRecord to)
        {
            if (_dailyReturns.LastItem == null || _dailyReturns.LastItem?.Key < to.Asof)
            {
                var metaInfo = new DailyReturnMetaInfo(from, to, _calculationHandler.CalcAbsoluteReturn(from, to));
                _dailyReturns.Add(to.Asof, metaInfo);
            }
        }


        public void AddMonthlyUltimoReturn(ITradingRecord ultimoRecord)
        {
            _monthlyReturns.Add(new Tuple<DateTime, decimal>(ultimoRecord.Asof, _calculationHandler.CalcAbsoluteReturn(LastUltimoRecord, ultimoRecord)));
            LastUltimoRecord = ultimoRecord;
        }

        public bool ScanRange(DateTime backtestDateTime, DateTime startDateInput)
        {
            return _calculationHandler.ScanRange(_priceHistory.Range(backtestDateTime, startDateInput));
        }

        public bool ScanRangeNoLow(DateTime backtestDateTime, DateTime startDateInput)
        {
            var vola = _priceHistory.Calc.GetVolatilityMonthly(backtestDateTime, startDateInput);
            return _calculationHandler.ScanRangeNoLow(_priceHistory.Range(backtestDateTime, startDateInput), vola);
        }

        public decimal GetVolatilityMonthly(DateTime? from, DateTime? to = null, CalculationOption? opt = null, PriceHistoryOption priceHistoryOption = PriceHistoryOption.PreviousItem)
        {
            var start = from ?? FirstRecord.Asof;
            var end = to ?? LastRecord.Asof;

            return _calculationHandler.CalcVolatility(EnumMonthlyReturns(), opt ?? CalculationOption.Adjusted);
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
            //ITradingRecord first = null;
            //foreach (var record in _priceHistory.EnumMonthlyUltimoItems())
            //{
            //    if (first == null)
            //    {
            //        first = record;
            //        continue;
            //    }

            //    yield return GetAbsoluteReturn(first.Asof, record.Asof);
            //    first = record;

            //}

            ////damit kann gleich nach MOnaten gruppiert werden mit dem Key "{MMYY}"
            foreach (var grp in _dailyReturns.GroupBy(x => new { x.Key.Month, x.Key.Year }))
                yield return grp.Sum(y => y.Value.AbsoluteReturn);
        }

        public IEnumerable<Tuple<DateTime, decimal>> EnumDailyReturnsTuple()
        {
            foreach (var item in _dailyReturns)
                yield return Tuple.Create(item.Key, item.Value.AbsoluteReturn);
        }

        public void CalcRunningArithmeticMean(ITradingRecord item)
        {
            if (_priceHistory.Count == 1)
                _arithmeticMean = item.AdjustedPrice;
            else
                _arithmeticMean = (item.AdjustedPrice + _arithmeticMean) / _priceHistory.Count;
        }


        public bool TryGetLastVolatility(DateTime asof, out decimal? volatility)
        {
            volatility = null;
            if (!_movingVolaMetaInfos.TryGetLastItem(asof, out var metaInfo))
                return false;

            volatility = metaInfo.DailyVolatility;
            return true;
        }


        public void CalcMovingVola(ITradingRecord item)
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

                // ------------ Achtung bei Kursen wo bespw. nur ein Kurs ist und dann Jahrelange nichts kann ich das so fixen
                if ((_dailyReturns.LastItem.Key - _dailyReturns.FirstItem.Key).Days < 250)
                {
                    return;
                }

                //einmal alle Returns druchlaufen und den Average berechnen
                var averageReturn = EnumDailyReturns().Average();

                //danach nich einmal durchlaufen und die varianz rechnen
                foreach (var dailyReturn in EnumDailyReturns())
                {
                    variance += Math.Pow((double)dailyReturn - (double)averageReturn, 2);
                }

                //dann brauche ich den eigentlichen Count nicht injecten
                _movingVolaMetaInfos.Add(item.Asof, new MovingVolaMetaInfo(averageReturn, (decimal)variance, _priceHistory.Settings, _priceHistory.Count));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void CalcAbsoluteLossesAndGains(ITradingRecord item)
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

        private Dictionary<int, HistogrammCollection> _rollingResultsDictionary;

        /// <summary>
        /// Der Task berechnet die Rollierenden Ergebnisse auf Basis der PriceHistoryCollection
        /// </summary>
        /// <param name="periodesInYears">die perioden in Yahren die berechnet werden sollen</param>
        /// <returns></returns>
        public Task CreateRollingPeriodeResultsTask(int[] periodesInYears)
        {
            return Task.Run(() =>
            {
                //einmalig erstellen
                if (_rollingResultsDictionary == null)
                    _rollingResultsDictionary = new Dictionary<int, HistogrammCollection>();

                //ich geh die PriceHistory einmal durch und berechne gleich alle möglichen Performancezeiträume
                for (var i = 0; i < _priceHistory.Count - 1; i++)
                {
                    var currentRecord = _priceHistory.Get(i);
                    foreach (var currentPeriode in periodesInYears)
                    {
                        var toDate = currentRecord.Asof.AddYears(currentPeriode);
                        if (toDate >= _priceHistory.LastItem.Asof)
                            continue;

                        var perfAbsolute = _calculationHandler.CalcAbsoluteReturn(currentRecord, _priceHistory.Get(toDate));
                        var perfCompound = _calculationHandler.CalcAverageReturn(currentRecord, _priceHistory.Get(toDate));

                        if (!_rollingResultsDictionary.TryGetValue(currentPeriode, out _))
                            _rollingResultsDictionary.Add(currentPeriode, new HistogrammCollection());
                        _rollingResultsDictionary[currentPeriode].Add(new PeriodeResult(currentPeriode, perfAbsolute, perfCompound, currentRecord.Asof, toDate));
                    }
                }
            });
        }

        public IEnumerable<IEnumerable<IHistogrammCollection>> EnumHistogrammClasses()
        {
            foreach (var kvp in _rollingResultsDictionary)
            {
                yield return kvp.Value.EnumHistogrammClasses();
            }
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

        public void CalcMovingLows(ITradingRecord item)
        {
            //brauche erst rechnen ab dem Moment wo sich ein erstes Fenster ausgeht
            if (_priceHistory.Count < _priceHistory.Settings.MovingAverageLengthInDays)
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
                    //das High aktualisieren
                    if (item.AdjustedPrice > lastLowMetaInfo.High?.AdjustedPrice)
                        lastLowMetaInfo.UpdateHigh(item, lastLowMetaInfo.NewHighsCount++);
                    
                    //manipulation des letzten Eintrags
                    lastLowMetaInfo.UpdatePeriodeRecords(item);

                    //das Item von vor 150 Tagen
                    //var newFirst = _priceHistory.Get(item.Asof.AddDays(-_priceHistory.Settings.MovingAverageLengthInDays));

                    //wenn der aktuelle Preis höher ist als der vorherige kann es kein neues low geben
                    if (item.AdjustedPrice > lastLowMetaInfo.Last.AdjustedPrice)
                    {
                        //merke mir das item mit hasNewlow=false
                        _lowMetaInfos.Add(item.Asof, new LowMetaInfo(lastLowMetaInfo.Low, item, lastLowMetaInfo, false));
                        return;
                    }

                    //wenn das letze Low tiefer liegt, und das datum des Lows noch in der Range ist brauche ich die 150 Tage nur um eines weiterschieben
                    if (lastLowMetaInfo.Low.AdjustedPrice < item.AdjustedPrice && lastLowMetaInfo.Low.Asof >= lastLowMetaInfo.First?.Asof)
                    {
                        //merke mir das item mit hasNewlow=false
                        _lowMetaInfos.Add(item.Asof, new LowMetaInfo(lastLowMetaInfo.Low, item, lastLowMetaInfo, false));
                        return;
                    }
                }

                var countNewHighs = 0;
                //neu berechnen
                foreach (var record in _priceHistory.Range(item.Asof.AddDays(-_priceHistory.Settings.MovingAverageLengthInDays), item.Asof))
                {
                    records.Add(record);

                    if (low == null)
                    {
                        //merke mir hier den ersten
                        low = new CalculationRecordMetaInfo(record);
                        first = new CalculationRecordMetaInfo(record);
                        high = new CalculationRecordMetaInfo(record);
                    }

                    //dann gibt es ein neues Low
                    if (record.AdjustedPrice < low.AdjustedPrice)
                        low = new CalculationRecordMetaInfo(record);
                    //auch das high merken
                    if (record.AdjustedPrice > high.AdjustedPrice)
                    {
                        countNewHighs++;
                        high = new CalculationRecordMetaInfo(record);
                    }

                    //merke mir hier immer den letzten Record
                    last = record;
                }
                //wenn lastLowMetaInfo == null bin ich beim ersten Record
                _lowMetaInfos.Add(item.Asof, lastLowMetaInfo != null
                        ? new LowMetaInfo(first, low, last, lastLowMetaInfo, true)
                        : new LowMetaInfo(low, last, records));

                //das high nachziehen
                _lowMetaInfos.LastItem.Value.UpdateHigh(high, countNewHighs);
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


        public bool TryGetLastVolatilityInfo(DateTime currentDate, out IMovingVolaMetaInfo movingVolaMeta)
        {
            movingVolaMeta = null;
            if (_movingVolaMetaInfos.TryGetLastItem(currentDate, out var vola))
                movingVolaMeta = vola;
            return movingVolaMeta != null;
            // return _movingVolaMetaInfos.TryGetLastItem(currentDate, out var vola) ? (movingVolaMeta = vola as IMovingVolaMetaInfo) : (movingVolaMeta = null);
        }

        public IEnumerable<(DateTime dateTime, ILowMetaInfo metaInfo)> EnumLows()
        {
            foreach (var kvp in _lowMetaInfos)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }
    }

    public class HistogrammCollection : List<PeriodeResult>, IHistogrammCollection
    {

        public HistogrammCollection(int classCount = 5)
        {
            ClassCount = classCount;
        }

        public HistogrammCollection(IEnumerable<PeriodeResult> results, int periodeInYears, decimal relativeFrequency)
        {
            PeriodeInYears = periodeInYears;
            RelativeFrequency = relativeFrequency;
            AddRange(results);
        }

        /// <summary>
        /// das Maximum
        /// </summary>
        public IPeriodeResult Maximum => this.OrderByDescending(x => x.Performance).FirstOrDefault();

        /// <summary>
        /// Das Minimum
        /// </summary>
        public IPeriodeResult Minimum => this.OrderByDescending(x => x.Performance).LastOrDefault();

        /// <summary>
        /// Bestimmt die Klassenbreite
        /// </summary>
        public int ClassCount { get; }

        /// <summary>
        /// die Periode für die Rollierende Berechnung
        /// </summary>
        public int PeriodeInYears { get; }

        /// <summary>
        /// die Relative Häufigkeit der Klasse
        /// </summary>
        public decimal RelativeFrequency { get; }

        /// <summary>
        /// der Count der Collection
        /// </summary>
        int IHistogrammCollection.Count => this.Count;

        /// <summary>
        /// Enumeriert die aktuelle Klasse und zieht das minimum und maximum immer nach
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IHistogrammCollection> EnumHistogrammClasses()
        {
            var span = Maximum.Performance - Minimum.Performance;
            var classWidth = span / ClassCount;
            var currentMax = 0M;
            var currentMin = Minimum.Performance;
            for (var i = 0; i < ClassCount; i++)
            {
                if (currentMax == 0)
                    currentMax = Minimum.Performance + classWidth;
                else
                {
                    currentMin = currentMax;
                    currentMax += classWidth;
                }

                var result = this.OrderByDescending(x => x.Performance).Where(x => x.Performance < currentMax && x.Performance > currentMin).ToList();
                var rel = (decimal)result.Count / this.Count;
                yield return new HistogrammCollection(result, this[0].RollingPeriodeInYears, rel);
            }
        }

        IEnumerable<IHistogrammCollection> IHistogrammCollection.EnumHistogrammClasses()
        {
            throw new NotImplementedException();
        }
    }
}