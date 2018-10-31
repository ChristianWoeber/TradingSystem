using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HelperLibrary.Calculations;
using HelperLibrary.Collections;
using HelperLibrary.Database.Models;
using HelperLibrary.Extensions;
using HelperLibrary.Parsing;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    public class ExposureWatcher : IExposureProvider
    {
        private readonly IExposureReceiver _receiver;
        private readonly PriceHistoryCollection _benchmark;
        private int _currentStep;

        public enum IndexType
        {
            Dax,
            EuroStoxx50,
            MsciWorldEur,
        }

        public ExposureWatcher(IExposureReceiver receiver, IPortfolioSettings settings, IndexType type)
        {
            _receiver = receiver;
            var files = Directory.GetFiles(settings.IndicesDirectory);

            var tradingRecords = SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(
                files.FirstOrDefault(x => x.ContainsIc(type.ToString())));

            _benchmark = new PriceHistoryCollection(tradingRecords, true);
        }

        public void CalculateMaximumExposure(DateTime asof)
        {
            //Wenn das Datum das Low der letzen 10 Tage ist reduziere ich die Aktienquote
            if (!_benchmark.TryGetLowMetaInfo(asof, out var lowMetaInfo))
                return;

            //wenn das Low schon hinter uns liegt und der Moving Average positiv ist
            if (lowMetaInfo.Low.Asof < asof && lowMetaInfo.MovingAverageDelta > 0 && lowMetaInfo.CanMoveToNextStep)
            {
                if (_currentStep <= 0)
                    return;
                if (_currentStep >= 1)
                    _currentStep--;
                UpdateMaximumRisk();
            }
            else if (lowMetaInfo.MovingAverageDelta < 0)
            {
                //sonst bin ich bei dem angefragten Asof
                //increment current Step
                if (_currentStep < NumberOfSteps)
                    _currentStep++;
                //calc new Targe Risk
                UpdateMaximumRisk();
            }
        }

        private void UpdateMaximumRisk()
        {
            var maxRisk = (1 - (decimal)_currentStep / NumberOfSteps);
            if (maxRisk >= _receiver.MinimumAllocationToRisk)
                //dannn setzte ich den aktuellen Wert
                _receiver.MaximumAllocationToRisk = maxRisk;
        }

        /// <summary>
        /// Gibt an wieviele Stufen zum Abstufen zur Verfügung stehen
        /// Bspw, => bei 4 => jeweils um 25% MaxAllocatationToRiskReduziert
        /// </summary>
        public int NumberOfSteps { get; set; } = 5;

        public IEnumerable<Tuple<DateTime, LowMetaInfo>> EnumLows()
        {
            return _benchmark.EnumLows();
        }
    }
}