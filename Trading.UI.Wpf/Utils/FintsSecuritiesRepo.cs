using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arts.Financial;
using Arts.Util;

namespace Trading.UI.Wpf.Utils
{
    public static class FintsSecuritiesRepo
    {
        public static Lazy<FINTS<double>> Eurostoxx50 { get; } = new Lazy<FINTS<double>>(LoadEuroStoxx50History);

        private static FINTS<double> LoadEuroStoxx50History()
        {
            return DBTools.ADBQueryFINTS<double>("INDEXQUOTES", 724, 1);
        }
    }
}
