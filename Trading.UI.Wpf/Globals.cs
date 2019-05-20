using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trading.UI.Wpf
{
    public static class Globals
    {
        public static string PriceHistoryFilePath { get; set; }
        public static string BasePath { get; set; }
        public static string IndicesBasePath { get; set; }
        public static string PriceHistoryDirectory { get; internal set; }
        public static string TransactionsDirectory { get; set; }
        public static string PortfolioValuationDirectory { get; set; }
        public static bool IsTestMode { get; set; } 
    }
}
