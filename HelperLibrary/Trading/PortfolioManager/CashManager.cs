using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HelperLibrary.Extensions;
using HelperLibrary.Util.Atrributes;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    /// <summary>
    /// Die Klasse kümmert sich um die Berechnug des Cash-Bestandes
    /// </summary>
    public class CashManager : ICashManager
    {
        #region private

        private readonly PortfolioManager _portfolioManager;
        private readonly IPortfolioSettings _settings;
        private decimal _cash;


        #endregion

        #region Constructor

        public CashManager(PortfolioManager portfolioManager)
        {
            _portfolioManager = portfolioManager;
            _settings = portfolioManager.PortfolioSettings;

        }

        #endregion

        public event EventHandler<DateTime> CashChangedEvent;

        public void CleanUpCash(List<ITradingCandidate> remainingCandidates, List<ITradingCandidate> remainingBestNotInvestedCandidates)
        {
            if (remainingCandidates == null)
                throw new ArgumentNullException(nameof(remainingCandidates));

            if (remainingCandidates.Count == 0)
                return;

            var targetCash = (1 - _portfolioManager.PortfolioSettings.MaximumAllocationToRisk) *
                             _portfolioManager.PortfolioValue + (_portfolioManager.PortfolioSettings.CashPufferSize * _portfolioManager.PortfolioValue);

            //die Temporären Kandidaten
            var tempToAdjust = remainingCandidates.Where(x => x.TransactionType != TransactionType.Close).ToList();
            //CleanUpTemporaryItems(tempToAdjust);

            //Hier bin nich mit dem Rebalancing fertig
            if (Cash < targetCash)
            {
                //Cash Clean Up der temporären auf Puffer Größe
                if (tempToAdjust.Count > 0)
                {
                    for (var i = tempToAdjust.Count - 1; i >= 0; i--)
                    {
                        if (Cash >= targetCash || Cash > 0 && Cash < 1500)
                            break;
                        //ist der aktuell schlechtest Kandiat
                        var current = tempToAdjust[i];
                        tempToAdjust.Remove(current);

                        var missingCash = Cash;

                        if (Cash > 0)
                        {
                            missingCash = Cash - targetCash;
                        }

                        if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(missingCash, current,
                            !current.IsInvested))
                            break;
                    }
                }
            }

            if (Cash < 0)
            {
                var ids = _portfolioManager.TemporaryPortfolio.Where(x => x.IsTemporary).Select(x => x.SecurityId);
                var candidates = _portfolioManager.TemporaryCandidates
                    .Where(x => ids.Any(id => id == x.Key)).Select(kvp => kvp.Value).Where(value => value.TransactionType != TransactionType.Close)
                    .OrderByDescending(x => x.ScoringResult.Score)
                    .ToList();

                for (var i = candidates.Count - 1; i >= 0; i--)
                {
                    var current = candidates[i];
                    if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(Cash, current, !current.IsInvested))
                        break;
                }
            }
            else if (Cash > 0 && Cash < targetCash)
            {
                var missingCash = Cash - targetCash;
                if (missingCash > -1500)
                    return;

                //sonst muss ich die bestehende Position abschichten
                foreach (var secIdGrp in _portfolioManager.TemporaryPortfolio.GroupBy(x => x.SecurityId))
                {
                    if (secIdGrp.LastOrDefault()?.TransactionType == TransactionType.Close)
                        continue;

                    if (!_portfolioManager.TemporaryCandidates.TryGetValue(secIdGrp.Key, out var candidate))
                        throw new ArgumentException($"Achtung der Key ist nicht im TemporaryCandidates Dictionary{secIdGrp.Key}");

                    if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(missingCash, candidate, true))
                        return;

                }
            }
            else if (TryHasCash(out _))
            {
                //nur neue aufbauen wenn erlaubt
                var minBoundry = _settings.MaximumAllocationToRisk - _settings.AllocationToRiskBuffer;
                var maxBoundry = _settings.MaximumAllocationToRisk == 1 ? 1 : _settings.MaximumAllocationToRisk + _settings.AllocationToRiskBuffer;
                if (_portfolioManager.TemporaryPortfolio.CurrentSumInvestedTargetWeight.IsBetween(minBoundry, maxBoundry))
                    return;

                //Dann ist noch Cash für einen Kandiaten über
                foreach (var currentBestCandidate in remainingBestNotInvestedCandidates)
                {
                    //neuen kaufen
                    if (_portfolioManager.TemporaryPortfolio.ContainsCandidate(currentBestCandidate))
                        continue;

                    if (_portfolioManager.TemporaryPortfolio.CurrentSumInvestedTargetWeight.IsBetween(minBoundry, maxBoundry))
                        return;

                    //wenn ich kein Cash mehr habe breake ich an dieser Stelle
                    if (!TryHasCash(out _))
                    {
                        if (Cash > 5000 + targetCash)
                        {
                            //noch eine kleine positon aufbauen - sonst bin ich nie voll investiert
                            var weight = Math.Round((Cash - targetCash) / _portfolioManager.PortfolioValue, 4);
                            currentBestCandidate.TargetWeight = weight;
                            currentBestCandidate.TransactionType = TransactionType.Open;
                            _portfolioManager.AddToTemporaryPortfolio(currentBestCandidate);
                        }

                        break;

                    }
                    currentBestCandidate.TransactionType = TransactionType.Open;
                    currentBestCandidate.TargetWeight = _settings.MaximumInitialPositionSize;
                    _portfolioManager.AddToTemporaryPortfolio(currentBestCandidate);
                }
            }

        }

        private void CleanUpTemporaryItems(List<ITradingCandidate> tempToAdjust)
        {
            var missing = tempToAdjust.Where(x => !_portfolioManager.TemporaryPortfolio.ContainsCandidate(x) && !x.IsInvested);

            foreach (var candidate in missing)
            {
                Trace.TraceError($"Achtung folgender Kandidat wurde nicht im temporären Portfolio gefunden und wird daher  gefixed {Environment.NewLine + candidate}");
                _portfolioManager.AddToTemporaryPortfolio(candidate);
            }

        }

        //TODO: Testfälle implementieren
        public bool TryHasCash(out decimal remainingCash)
        {
            remainingCash = Cash;
            ////das relative gewicht
            var relativeWeight = remainingCash / _portfolioManager.PortfolioValue;


            //return HasCash
            return relativeWeight - _settings.MaximumInitialPositionSize >= 0;
        }


        /// <summary>
        /// der aktuelle Cash Bestand
        /// </summary>
        public decimal Cash
        {
            get => _cash;
            set
            {
                _cash = value;
                CashChangedEvent?.Invoke(this, _portfolioManager.PortfolioAsof);
                //AddToDictionary(_cash);
            }
        }

        private readonly Dictionary<DateTime, List<CashMetaInfo>> _cashValues = new Dictionary<DateTime, List<CashMetaInfo>>();

        private void AddToDictionary(decimal cash)
        {
            if (!_cashValues.TryGetValue(_portfolioManager.PortfolioAsof, out var _))
                _cashValues.Add(_portfolioManager.PortfolioAsof, new List<CashMetaInfo>());

            var infos = _cashValues[_portfolioManager.PortfolioAsof];
            //wenn der Count 0 ist handelt es sich um den StartSaldo
            //_cashValues[_portfolioManager.PortfolioAsof].Add(infos.Count == 0
            //    ? new CashMetaInfo(_portfolioManager.PortfolioAsof, cash, true)
            //    : new CashMetaInfo(_portfolioManager.PortfolioAsof, cash));
        }
    }

    public class CashMetaInfo
    {
        public CashMetaInfo()
        {

        }
        public CashMetaInfo(DateTime asof, decimal cashValue, bool isStartSaldo = false)
        {
            IsStartSaldo = isStartSaldo;
            Cash = cashValue;
            Asof = asof;
        }

        [InputMapping(KeyWords = new[] { nameof(Asof) })]
        public DateTime Asof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Cash) })]
        public decimal Cash { get; set; }

        public bool IsStartSaldo { get; set; }

        public bool IsEndSaldo { get; set; }

    }
}