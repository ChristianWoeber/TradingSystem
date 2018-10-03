using System;
using System.Collections.Generic;
using HelperLibrary.Database.Models;

namespace HelperLibrary.Interfaces
{
    /// <summary>
    /// Das Interface das den Speicher zur Verfügung stellt => im live Betrieb wird hier eine Datenbabk supplied zum Test ein csv File
    /// </summary>
    public interface ITransactionsCacheProvider
    {
        /// <summary>
        /// der Speicher mit den Transaktionen, nach SECID und der Liste von Transaktionen zu dem Wertpapier
        /// </summary>
        Lazy<Dictionary<int, List<Transaction>>> TransactionsCache { get; }

        /// <summary>
        /// Methode um den Sopeicher upzudaten
        /// </summary>
        void UpdateCache();
    }
}