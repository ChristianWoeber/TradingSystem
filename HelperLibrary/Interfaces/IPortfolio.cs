using System;
using System.Collections.Generic;
using HelperLibrary.Database.Models;

namespace HelperLibrary.Interfaces
{
    /// <summary>
    /// Das aktuelle Poertfolio, das auch per Indexer dusucht werden kann
    /// </summary>
    public interface IPortfolio : IEnumerable<Transaction>
    {
        /// <summary>
        /// gibt das TransactionItem auf Basis der Security Id zurück
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Transaction this[int key] { get; }


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