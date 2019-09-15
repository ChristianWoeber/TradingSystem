using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Trading.Calculation.Collections;
using Trading.Core.Backtest;
using Trading.Core.Extensions;
using Trading.Core.Settings;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;
using TradingRecord = Trading.Core.Models.TradingRecord;

namespace Trading.Core.Exposure
{
    public class ExposureWatcher : IExposureProvider
    {
        private readonly IExposureSettings _receiver;
        private readonly IPriceHistoryCollection _benchmark;
        private int _currentStep;


        public IExposureSettings GetExposure()
        {
            return _receiver;
        }

        public ExposureWatcher(IExposureSettings settings)
        {
            _receiver = settings;
            if (string.IsNullOrWhiteSpace(settings.IndicesDirectory))
            {
                Trace.TraceError("Achtung es wurde kein Pfad für die History der Indizes angegeben");
                return;
            }
            var files = Directory.GetFiles(settings.IndicesDirectory);

            var tradingRecords = SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(settings.IndexType == IndexType.SandP500
                ? files.FirstOrDefault(x => x.ContainsIc("S&P"))
                : files.FirstOrDefault(x => x.ContainsIc(settings.IndexType.ToString())));

            var calculationSettings = new PriceHistoryCollectionSettings(150, 0);
            _benchmark = PriceHistoryCollection.Create(tradingRecords, calculationSettings);
        }

        private decimal? _lastSimulationNav;
        public void CalculateIndexResult(DateTime asof)
        {
            if (_lastSimulationNav == null)
                _lastSimulationNav = 100;
            if (asof > _benchmark.LastItem.Asof)
                return;

            //berechne hier gleich den NAV der simulation
            var indexLevel = _benchmark.Get(asof);

            if (indexLevel == null)
                return;

            //wenn das  indexLevel.Asof < ist als das aktuelle muss ich 0 nehmen, (feiertage etc. da hat es keien veränderung gegeben)
            var dailyReturn = indexLevel.Asof < asof ? 0 : _benchmark.GetDailyReturn(indexLevel);
            ((IIndexBackTestResult)_receiver).Asof = asof;
            ((IIndexBackTestResult)_receiver).IndexLevel = indexLevel.AdjustedPrice;
            //der letzte NAV multipliziert mit der gewichteten dailyperformance => ist der neue NAV
            ((IIndexBackTestResult)_receiver).SimulationNav = Math.Round(_lastSimulationNav.Value * (1 + dailyReturn * _receiver.MaximumAllocationToRisk), 4);
            _lastSimulationNav = ((IIndexBackTestResult)_receiver).SimulationNav;

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
            if (maxRisk >= _receiver.MinimumAllocationToRisk)
                _receiver.MaximumAllocationToRisk = maxRisk;
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