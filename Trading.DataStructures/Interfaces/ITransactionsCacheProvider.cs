﻿using System;
using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Das Interface das den Speicher zur Verfügung stellt => im live Betrieb wird hier eine Datenbabk supplied zum Test ein csv File
    /// </summary>
    public interface ITransactionsCacheProvider
    {
        /// <summary>
        /// der Speicher mit den Transaktionen, nach SECID und der Liste von Transaktionen zu dem Wertpapier
        /// </summary>
        Lazy<Dictionary<int, List<ITransaction>>> TransactionsCache { get; }

        /// <summary>
        /// Methode um den Speicher upzudaten
        /// </summary>
        void UpdateCache();
    }
}