namespace Trading.Core.Extensions
{
    public static class DecimalExtensions
    {
        public static bool IsBetween(this decimal source, decimal minBoundary, decimal maxBoundary)
        {
            return source >= minBoundary && source <= maxBoundary;
        }
    }
}