using System;
using System.Collections.Generic;
using System.Linq;
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

        public void CleanUpCash(List<ITradingCandidate> allCandidates, List<ITradingCandidate> investedCandidates)
        {
            if (investedCandidates == null)
                throw new ArgumentNullException(nameof(investedCandidates));

            if (investedCandidates.Count == 0)
                return;

            //Hier bin nich mit dem Rebalancing fertig
            if (Cash < 0)
            {
                //die Temporären Kandidaten
                var tempToAdjust = allCandidates.Where(x => x.IsTemporary && x.TransactionType != TransactionType.Close)
                    .ToList();

                //Cash Clean Up der temporären auf Puffer Größe
                if (tempToAdjust.Count > 0)
                {
                    for (var i = tempToAdjust.Count - 1; i >= 0; i--)
                    {
                        var current = tempToAdjust[i];
                        if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(Cash, current, true))
                            break;
                    }
                }
                //cash clean up der investierten
                else
                {
                    var investedToAdjust = investedCandidates.Where(x => !x.IsTemporary).ToList();

                    for (var i = investedToAdjust.Count - 1; i >= 0; i--)
                    {
                        var current = investedToAdjust[i];
                        //sicherheitshalber nochmal checken ob nicht im temporären portfolio
                        if (_portfolioManager.TemporaryPortfolio.IsTemporary(current.Record.SecurityId))
                            continue;
                        current.TransactionType = TransactionType.Changed;
                        if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(Cash, current))
                            break;
                    }

                    if (Cash < 0)
                    {
                    }
                }
            }
            else if (TryHasCash(out var remainingCash))
            {
                //Dann ist noch Cash für einen Kandiaten über
            }

        }

        //TODO: Testfälle implementieren
        public bool TryHasCash(out decimal remainingCash)
        {
            remainingCash = Cash;
            ////das relative gewicht
            var relativeWeight = remainingCash / _portfolioManager.PortfolioValue;


            //return HasCash
            return relativeWeight - _settings.MaximumInitialPositionSize > 0;
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
    }
}