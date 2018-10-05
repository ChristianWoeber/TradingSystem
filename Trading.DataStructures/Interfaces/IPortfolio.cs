using System;
using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Das aktuelle Poertfolio, das auch per Indexer dusucht werden kann
    /// </summary>
    public interface IPortfolio : IEnumerable<ITransaction>
    {
        /// <summary>
        /// gibt das TransactionItem auf Basis der Security Id zurück
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        ITransaction this[int key] { get; }

        /// <summary>
        /// Flag ob es Items gibt
        /// </summary>
        bool HasItems(DateTime asof);

        /// <summary>
        /// das Flag das angibt, ob das CurrentPortfolio bereits initialisiert wurde
        /// </summary>
         bool IsInitialized { get; }

    }
}