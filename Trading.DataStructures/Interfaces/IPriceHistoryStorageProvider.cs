using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    public interface IPriceHistoryStorageProvider
    {
        /// <summary>
        /// Storage of alle price histories, key => id, value the IPriceHistoryCollection
        /// </summary>
        IDictionary<int, IPriceHistoryCollection> PriceHistoryStorage { get; }

    }
}