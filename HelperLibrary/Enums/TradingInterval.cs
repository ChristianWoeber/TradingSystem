using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Enums
{
    public enum TradingInterval
    {
        /// <summary>
        /// Enum für wöchentlichen Trading Zyklus
        /// </summary>
        weekly,
        /// <summary>
        /// Enum für alle 2 Wochen Trading Zyklus
        /// </summary>
        twoWeeks,
        /// <summary>
        /// Enum für alle 3 Wochen Trading Zyklus
        /// </summary>
        threeWeeks,
        /// <summary>
        /// Enum für einmal pro Monat Trading Zyklus
        /// </summary>
        monthly,
    }
}
