using System;

namespace HelperLibrary.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsIC(this string source, string toCeck)
        {
            return source.IndexOf(toCeck, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
