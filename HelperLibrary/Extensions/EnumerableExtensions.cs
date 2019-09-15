using System;
using System.Collections.Generic;

namespace HelperLibrary.Extensions
{
    public static class EnumerableExtensions
    {
        public static Dictionary<TKey, List<T>> ToDictionaryList<TKey, T>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        {
            var ret = new Dictionary<TKey, List<T>>();
            if (source == null)
                return null;

            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            foreach (var s in source)
            {
                List<T> val;
                var key = keySelector(s);
                if (!ret.TryGetValue(key, out val))
                    ret.Add(key, val = new List<T>());

                val.Add(s);
            }
            return ret;
        }
    }
}
