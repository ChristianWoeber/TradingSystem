using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Yahoo
{
    public static class YahooFieldsList
    {
        public static string SYMBOL => "s";
        public static string CRUMB => "crumb";
        public static string EQUALS => "=";
        public static string HISTORY => "events=history";
        public static string START_MONTH => "a";
        public static string START_DAY => "b";
        public static string START_YEAR => "c";
        public static string END_MONTH => "d";
        public static string END_DAY => "e";
        public static string END_YEAR => "f";
        public static string INTERVAL=> "interval";
        public static string TICKER_PLACEHOLDER => "@";
        public static string AND => "&";
        public static string QUESTIONMARK => "?";
        public static string FROM => "period1";
        public static string TO => "period2";

      
    }
}
