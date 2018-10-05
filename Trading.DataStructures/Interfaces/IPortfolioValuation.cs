using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trading.DataStructures.Interfaces
{
    public interface IPortfolioValuation
    {
        DateTime PortfolioAsof { get; set; }

        /// <summary>
        /// Der Portfoliowert - wird initial mit 100 angemommen, sonfern kein Betrag gegeben ist
        /// </summary>
        decimal PortfolioValue { get; set; }


        /// <summary>
        /// Die Aktienquote, bzw. die Allokation der Wertpapiere in %
        /// </summary>
        decimal AllocationToRisk { get; set; }
    }
}
