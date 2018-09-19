using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Extensions
{
    public static class DecimalExtensions
    {
        public static bool IsBetween(this decimal source, decimal minBoundary, decimal maxBoundary)
        {
            return source >= minBoundary && source <= maxBoundary;
        }

    }
}
