using System;
using HelperLibrary.Util.Atrributes;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Cash
{
    /// <summary>
    /// Hilfsklasse zum Ausgeben und Loggen das Cashs
    /// </summary>
    public class CashMetaInfo: IInputMappable
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