using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Yahoo
{
    public enum YahooRequestInterval
    {
        [YahooChar("w")]
        Weekly,
        [YahooChar("d")]
        Daily,
        [YahooChar("m")]
        Monthly,
    }

    public class YahooChar : Attribute
    {
        public string Name { get; private set; }
        public YahooChar(string val)
        {
            Name = val;
        }
    }
}
