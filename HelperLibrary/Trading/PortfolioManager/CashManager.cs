using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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

        public void CleanUpCash(List<ITradingCandidate> remainingCandidates)
        {
            if (remainingCandidates == null)
                throw new ArgumentNullException(nameof(remainingCandidates));

            if (remainingCandidates.Count == 0)
                return;

            var targetCash = _portfolioManager.PortfolioSettings.CashPufferSize *
                             _portfolioManager.PortfolioSettings.MaximumAllocationToRisk *
                             _portfolioManager.PortfolioValue;

            //Hier bin nich mit dem Rebalancing fertig
            if (Cash < 0)
            {
                //die Temporären Kandidaten
                var tempToAdjust = remainingCandidates.Where(x => x.IsTemporary && x.TransactionType != TransactionType.Close).ToList();

                //Cash Clean Up der temporären auf Puffer Größe
                if (tempToAdjust.Count > 0)
                {
                    for (var i = tempToAdjust.Count - 1; i >= 0; i--)
                    {
                        if (Cash >= targetCash)
                            break;

                        var current = tempToAdjust[i];
                        tempToAdjust.Remove(current);
                        if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(Cash, current, true))
                            break;
                    }
                }
                //cash clean up der investierten
                else
                {
                    //var investedToAdjust = investedCandidates.Where(x => !x.IsTemporary).ToList();

                    //for (var i = investedToAdjust.Count - 1; i >= 0; i--)
                    //{
                    //    var current = investedToAdjust[i];
                    //    //sicherheitshalber nochmal checken ob nicht im temporären portfolio
                    //    if (_portfolioManager.TemporaryPortfolio.IsTemporary(current.Record.SecurityId))
                    //        continue;
                    //    current.TransactionType = TransactionType.Changed;
                    //    if (_portfolioManager.AdjustTemporaryPortfolioToCashPuffer(Cash, current))
                    //        break;
                    //}

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