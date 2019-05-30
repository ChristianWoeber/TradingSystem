using System;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Settings
{
    public class StopLossMeta : IStopLossMeta
    {
        public StopLossMeta(decimal price, DateTime asof)
        {
            Opening = new Record(price, asof);
            PreviousLow = new Record(price, asof);
            LocalLow = new Record(price, asof);
            High = new Record(price, asof);
        }

        /// <summary>
        /// Der Preis der eröffnung
        /// </summary>
        public IPriceRecord Opening { get; set; }

        /// <summary>
        /// Das niedrigste Low
        /// </summary>
        public IPriceRecord PreviousLow { get; private set; }

        /// <summary>
        /// Das aktuell höchste Low
        /// </summary>
        public IPriceRecord LocalLow { get; private set; }

        /// <summary>
        /// Das High seit Eröffnung
        /// </summary>
        public IPriceRecord High { get; private set; }


        public void UpdateHigh(decimal? price, DateTime asof)
        {
            High = new Record(price.Value, asof);
        }

        public void UpdateLocalLow(decimal? price, DateTime asof)
        {
            LocalLow = new Record(price.Value, asof);
        }

        public void UpdatePreviousLow(decimal? price, DateTime asof)
        {
            PreviousLow = new Record(price.Value, asof);
        }

        public void UpdatePreviousLow(IPriceRecord rec)
        {
            PreviousLow = new Record(rec.Price, rec.Asof);
        }

    }
}