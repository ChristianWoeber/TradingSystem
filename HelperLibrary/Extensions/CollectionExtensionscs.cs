using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HelperLibrary.Extensions
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this Collection<T> baseCollection, IEnumerable<T> dataToAdd)
        {
            foreach (var item in dataToAdd)
            {
                baseCollection.Add(item);
            }
        }
    }
}
