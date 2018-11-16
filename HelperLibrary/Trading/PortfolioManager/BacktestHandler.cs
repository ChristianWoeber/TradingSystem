using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelperLibrary.Extensions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    public class IndexResult : IIndexBackTestResult
    {
        public IndexResult()
        {
            
        }

        public IndexResult(IExposureReceiver receiver)
        {
            MaximumAllocationToRisk = receiver.MaximumAllocationToRisk;
            MinimumAllocationToRisk = receiver.MinimumAllocationToRisk;
            Asof = ((IIndexBackTestResult) receiver).Asof;
            SimulationNav = ((IIndexBackTestResult)receiver).SimulationNav;
            IndexLevel = ((IIndexBackTestResult)receiver).IndexLevel;

        }

        public decimal MaximumAllocationToRisk { get; set; }
        public decimal MinimumAllocationToRisk { get; set; }

        public DateTime Asof { get; set; }

        public decimal SimulationNav { get; set; }

        public decimal IndexLevel { get; set; }

        public override string ToString()
        {
            return
                $"{Asof} {nameof(SimulationNav)}: {SimulationNav} {nameof(MaximumAllocationToRisk)}: {MaximumAllocationToRisk} {nameof(IndexLevel)} : {IndexLevel}";
        }
    }

    public interface IIndexBackTestResult : IExposureReceiver
    {
        DateTime Asof { get; set; }

        decimal SimulationNav { get; set; }

        decimal IndexLevel { get; set; }
    }

    public class BacktestHandler : IDisposable
    {
        private readonly IExposureProvider _exposureProvider;
        private readonly IIndexBackTestResult _output;
        private readonly CandidatesProvider _candidatesProvider;
        private readonly PortfolioManager _portfolioManager;
        private readonly ISaveProvider _saveProvider;

        public BacktestHandler(IExposureProvider exposureProvider)
        {
            _exposureProvider = exposureProvider;
            IndexResults = new List<IndexResult>();
        }

        public BacktestHandler(PortfolioManager pm, CandidatesProvider candidatesProvider, ISaveProvider saveProvider)
        {
            _portfolioManager = pm;
            _candidatesProvider = candidatesProvider;
            _saveProvider = saveProvider;
        }

        public List<IndexResult> IndexResults { get; }

        public async Task RunIndexBacktest(DateTime startDateTime, DateTime? endDateTime, CancellationToken? cancel = null)
        {
            _startDateTime = DateTime.Today.GetBusinessDay(false);
            await Task.Factory.StartNew(() => RunIndex(startDateTime, endDateTime), cancel ?? CancellationToken.None);
        }

        private void RunIndex(DateTime startDateTime, DateTime? endDateTime)
        {
            var start = startDateTime;
            while (start < endDateTime)
            {
                _exposureProvider.CalculateMaximumExposure(start);
                _exposureProvider.CalculateIndexResult(start);
                IndexResults.Add(new IndexResult(_exposureProvider.GetExposure()));
                start = start.AddDays(1);
            }
        }

        public async Task RunBacktest(DateTime startDateTime, DateTime? endDateTime, CancellationToken? cancel = null)
        {
            _startDateTime = DateTime.Today.GetBusinessDay(false);
            await Task.Factory.StartNew(() => Run(startDateTime, endDateTime), cancel ?? CancellationToken.None);
        }


        private DateTime _startDateTime;

        private DateTime? _lastNavDateTime;

        private void Run(DateTime startDateTime, DateTime? endDateTime)
        {
            var date = startDateTime;
            var end = endDateTime?.GetBusinessDay(false) ?? DateTime.Today;
            while (true)
            {
                if (date.IsUltimo() || date.IsBusinessDayUltimo())
                    Trace.TraceInformation("aktuelles Datum: " + date.ToShortDateString());
                if (date >= _startDateTime)
                    return;

                //dann ist das Ende des Backtest erreicht
                if (date >= endDateTime || date >= DateTime.Today.GetBusinessDay(false))
                    return;

                var candidates = _candidatesProvider.GetCandidates(date, PriceHistoryOption.NextItem)?.ToList();
                var asof = candidates?.OrderByDescending(x => x.Record.Asof).FirstOrDefault()?.Record.Asof;

                if (asof == null)
                {
                    //Datum erhöhen
                    date = date.GetBusinessDay();
                    continue;
                }

                // überprüfen ob für das Datum schon ein score berücksichtig wurde
                if (_lastNavDateTime == candidates.OrderByDescending(x => x.Record.Asof).FirstOrDefault()?.ScoringResult.Asof)
                {
                    //Datum erhöhen
                    date = date.GetBusinessDay();
                    continue;
                }

                //Hier nur die bestehenden Positionen evaluieren
                if (date.DayOfWeek != _portfolioManager.PortfolioSettings.TradingDay)
                {
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    {
                        //Datum erhöhen
                        date = date.GetBusinessDay();
                        continue;
                    }

                    RankCandidates(new List<ITradingCandidateBase>(), asof, ref date);
                    continue;
                }
                RankCandidates(candidates, asof, ref date);
            }
        }

        private void RankCandidates(List<ITradingCandidateBase> candidates, DateTime? asof, ref DateTime date)
        {
            //NAVDatum initieren
            if (_lastNavDateTime == null)
            {
                _lastNavDateTime = candidates.OrderByDescending(x => x.Record.Asof).FirstOrDefault()?.ScoringResult.Asof ??
                                   DateTime.MinValue;
            }

            _portfolioManager.PassInCandidates(candidates, asof.Value);

            if (_portfolioManager.HasChanges)
            {
                _portfolioManager.TemporaryPortfolio.SaveTransactions(_saveProvider);
                _portfolioManager.TransactionsHandler.UpdateCache();
            }

            //Nav Datum nachziehen
            _lastNavDateTime = asof;
            //Datum erhöhen
            date = date.GetBusinessDay();
        }

        public void Dispose()
        {
            //Clean up

            var files = Directory.GetFiles(_portfolioManager.PortfolioSettings.LoggingPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}