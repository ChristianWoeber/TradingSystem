using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Extensions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Cash
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

            //das ZielCash inklusive Puffer
            var targetCash = (1 - _portfolioManager.PortfolioSettings.MaximumAllocationToRisk) *
                             _portfolioManager.PortfolioValue + (_portfolioManager.PortfolioSettings.CashPufferSize * _portfolioManager.PortfolioValue);

            //Hier bin nich mit dem Rebalancing fertig
            if (Cash < targetCash)
            {
                for (var i = remainingCandidates.Count - 1; i >= 0; i--)
                {
                    if (Cash >= targetCash || Cash > 0 && Cash < 1500)
                        return;

                    if (_portfolioManager.TemporaryPortfolio.CurrentSumInvestedTargetWeight.IsBetween(_portfolioManager.MinimumBoundary, _portfolioManager.MaximumBoundary))
                        return;

                    //ist der aktuell schlechtest Kandiat
                    var current = remainingCandidates[i];
                    remainingCandidates.Remove(current);

                    var missingCash = Cash;

                    if (Cash > 0)
                        missingCash = Cash - targetCash;
                    // ich gebe ja immer die Remaining Candidates mit und die sind per Definition schon im Temporären Portfolio, daher immer true hier und die aktuellen Positionen anpassen
                    if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(missingCash, current, true))
                        break;
                }
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
            }
        }

        private readonly Dictionary<DateTime, List<CashMetaInfo>> _cashValues = new Dictionary<DateTime, List<CashMetaInfo>>();

    }
}