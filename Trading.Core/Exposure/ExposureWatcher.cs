using System;
using System.Collections.Generic;
using Trading.Calculation.Collections;
using Trading.Core.Backtest;
using Trading.Core.Settings;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Exposure
{
    /// <summary>
    /// Der Exposure Watcher bekommt die Settings und Dataprovider als dependency injected
    /// auf diese Weise bleibt die klasse flexibel und es ist egal ob die Records die für das
    /// bewerten der Allocation To Risk benötigt werden aus einer Filestruktur oder aus einer Datenbank kommen
    /// </summary>
    public class ExposureWatcher : IExposureProvider
    {
        private readonly IExposureSettings _settings;
        private readonly IExposureDataProvider _dataProvider;
        private readonly IPriceHistoryCollection _benchmark;
        private int _currentStep;


        public IExposureSettings GetSettings()
        {
            return _settings;
        }

        public ExposureWatcher(IExposureSettings settings, IExposureDataProvider dataProvider)
        {
            _settings = settings;
            _dataProvider = dataProvider;

            var tradingRecords = _dataProvider.GetExposureRecords();
            if (tradingRecords == null)
                throw new ArgumentNullException(nameof(tradingRecords), "Achtung es konnten keine Records gefunden werden");

            var calculationSettings = new PriceHistoryCollectionSettings(150, 0);
            _benchmark = PriceHistoryCollection.Create(tradingRecords, calculationSettings);
        }

        private decimal? _lastSimulationNav;

        public void CalculateIndexResult(DateTime asof)
        {
            if (_lastSimulationNav == null)
                _lastSimulationNav = 100;

            if (_benchmark.LastItem == null)
                throw new ArgumentNullException(nameof(_benchmark.LastItem), "Achtung das LastItem der Prichistory is null! ");

            if (asof > _benchmark.LastItem.Asof)
                return;

            //berechne hier gleich den NAV der simulation
            var indexLevel = _benchmark.Get(asof);

            if (indexLevel == null)
                return;

            //wenn das  indexLevel.Asof < ist als das aktuelle muss ich 0 nehmen, (feiertage etc. da hat es keine veränderung gegeben)
            var dailyReturn = indexLevel.Asof < asof ? 0 : _benchmark.GetDailyReturn(indexLevel);
            ((IIndexBackTestResult)_settings).Asof = asof;
            ((IIndexBackTestResult)_settings).IndexLevel = indexLevel.AdjustedPrice;
            //der letzte NAV multipliziert mit der gewichteten dailyperformance => ist der neue NAV
            ((IIndexBackTestResult)_settings).SimulationNav = Math.Round(_lastSimulationNav.Value * (1 + dailyReturn * _settings.MaximumAllocationToRisk), 4);
            _lastSimulationNav = ((IIndexBackTestResult)_settings).SimulationNav;

        }

        public void CalculateMaximumExposure(DateTime asof)
        {
            //Wenn das Datum das Low der letzen 150 Tage ist reduziere ich die Aktienquote
            if (!_benchmark.TryGetLowMetaInfo(asof, out var lowMetaInfo))
                return;

            //wenn das Low schon hinter uns liegt und der Moving Average positiv ist
            if (lowMetaInfo.Low.Asof < asof && lowMetaInfo.MovingAverageDelta > 0 && lowMetaInfo.CanMoveToNextStep)
            {
                //hier erhöhe ich die Aktienquote
                if (_currentStep <= 0)
                    return;

                //ich erhöhe nur an Trading Tagen
                //if (asof.DayOfWeek != _receiver.TradingDay)
                //    return;

                if (_currentStep >= 1)
                    _currentStep--;
                UpdateMaximumRisk();
            }
            //Achtung ich reduziere die Aktienquoten nur bei einem neuen Low in dem moving Abschnitt
            else if (lowMetaInfo.MovingAverageDelta < 0 && lowMetaInfo.HasNewLow)
            {
                //hier reduziere ich die Aktienquote
                if (_currentStep < NumberOfSteps)
                    _currentStep++;
                //calc new Targe Risk
                UpdateMaximumRisk();
            }
        }

        private void UpdateMaximumRisk()
        {
            var maxRisk = (1 - (decimal)_currentStep / NumberOfSteps);
            //dannn setzte ich den aktuellen Wert
            if (maxRisk >= _settings.MinimumAllocationToRisk)
                _settings.MaximumAllocationToRisk = maxRisk;
        }

        /// <summary>
        /// Gibt an wieviele Stufen zum Abstufen zur Verfügung stehen
        /// Bspw, => bei 4 => jeweils um 25% MaxAllocatationToRiskReduziert
        /// </summary>
        public int NumberOfSteps { get; set; } = 5;

        public IEnumerable<(DateTime dateTime, ILowMetaInfo metaInfo)> EnumLows()
        {
            return _benchmark.EnumLows();
        }
    }
}