using System;
using System.Linq;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;

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

        //TODO: Cash und PortfolioValue verknüpfen
        public bool TryHasCash(out decimal remainingCash)
        {
            //Summiere die effektiven Gewichte der Positionen (wenn es verkäufe gibt ziehe ich diese ab)
            //var investedCash = GetCurrentCash();

            ////das frei verfügbare Cash
            //remainingCash = _portfolioManager.PortfolioValue - investedCash;

            remainingCash = Cash;
            ////das relative gewicht
            var relativeWeight = remainingCash / _portfolioManager.PortfolioValue;

            //TODO: MinimumPositionSize festlegen, die wenn der PortfolioWert steigt und sich eine 10% nicht ausgeht das minmum in EUR für die Transaktion festlegt

            //return HasCash
            return relativeWeight - _settings.MaximumInitialPositionSize > 0;
        }

        private decimal GetCurrentCash()
        {
            //TODO: Ich muss hier überlegen wie ich die minus gewichte berücksicktige target weight 0 müsste eigenlicht das Delta sein als o -0,1 z.B.:
            //TODO: ist es besser das Cash über den Amount EUR zu rechnen? bzw. den effective Amount...

            decimal sum = 0;
            //Summiere die Target Gewichte der Positionen (wenn es verkäufe gibt ziehe ich diese ab, und gruppiere nach secId um zu sehen ob es da bereits einen totalverkauf gab)
            foreach (var grp in _portfolioManager.TemporaryPortfolio.GroupBy(x => x.SecurityId))
            {
                //if (grp.Any(x => x.TransactionType == (int)TransactionType.Close))
                //    continue;
                sum += grp.Sum(x => x.EffectiveAmountEur);
            }

            return sum;
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
                CashChangedEvent?.Invoke(this,_portfolioManager.PortfolioAsof);
            }
        }
    }
}