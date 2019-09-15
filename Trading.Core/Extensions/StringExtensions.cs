using System;

namespace Trading.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsIc(this string source, string toCeck)
        {
            return source.IndexOf(toCeck, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
