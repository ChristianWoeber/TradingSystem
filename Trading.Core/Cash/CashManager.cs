using System;
using System.Collections.Generic;
using Trading.Core.Extensions;
using Trading.Core.Portfolio;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Cash
{
    /// <summary>
    /// Die Klasse kümmert sich um die Berechnug des Cash-Bestandes
    /// </summary>
    //TODO: Testfälle implementieren
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

        #region Events

        public event EventHandler<DateTime> CashChangedEvent;


        #endregion

        #region Clean Up

        public void CleanUpCash(List<ITradingCandidate> temporaryCandidates)
        {
            //TODO: IM CASH MANAGER wird manchmal der TransactionState falhsc manipuliert!! das muss dringend gefixed und getestet werden

            if (temporaryCandidates == null)
                throw new ArgumentNullException(nameof(temporaryCandidates));

            if (temporaryCandidates.Count == 0)
                return;

            //das ZielCash inklusive Puffer
            var targetCash = (1 - _portfolioManager.PortfolioSettings.MaximumAllocationToRisk) *
                             _portfolioManager.PortfolioValue + (_portfolioManager.PortfolioSettings.CashPufferSizePercent * _portfolioManager.PortfolioValue);

            //Hier bin nich mit dem Rebalancing fertig
            if (Cash < targetCash)
            {
                for (var i = temporaryCandidates.Count - 1; i >= 0; i--)
                {
                    //Das ist meine Abbruchbedingung wenn ich der Range bin oder kleiner als die MinimumBoundary
                    if ((_portfolioManager.CurrentSumInvestedEffectiveWeight.IsBetween(_portfolioManager.MinimumBoundary, _portfolioManager.MaximumBoundary)
                        || _portfolioManager.CurrentSumInvestedEffectiveWeight <= _portfolioManager.MinimumBoundary) && Cash > 0)
                        return;

                    //ist der aktuell schlechteste Kandiat und der wird auch gleich removed
                    var current = temporaryCandidates[i];
                    //um sicherzustellen, dass er nicht doppelt abgeschichtet wird
                    temporaryCandidates.Remove(current);

                    //Wenn ich hier false zurückbekomme bin ich fertig
                    if (!NeedToMoveNext(current))
                        return;
                }
            }
        }

        private bool NeedToMoveNext(ITradingCandidate candiate)
        {
            //die Investtionsqupte die zuviel ist
            var missingPercent = Math.Round(_portfolioManager.CurrentSumInvestedEffectiveWeight - _portfolioManager.MinimumBoundary, 4);

            //eigentlich sollte ich an dieser Stelle immer nur abschichten
            if (missingPercent <= 0)
                return false;

            //ich erhöhe noch um den CashPuffer
            missingPercent += _portfolioManager.PortfolioSettings.CashPufferSizePercent;

            //den candidaten anpassen
            _portfolioManager.AdjustTemporaryPortfolioToRiskBoundary(missingPercent, candiate);

            //dann iterirer ich weiter
            return true;

        }

        #endregion

        #region Cash & TryHasCash



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

        #endregion

    }
}