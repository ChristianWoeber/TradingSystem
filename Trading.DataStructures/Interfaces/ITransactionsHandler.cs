﻿using System;
using System.Collections.Generic;
using Trading.DataStructures.Enums;

namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Interface für den TransactionsHandler im PortfolioManager
    /// </summary>
    public interface ITransactionsHandler
    {
        /// <summary>
        /// gibt immer das aktuelle Portfolio zurück => die aktuellen Holdings (aufsummierte Positionen die nicht geschlossen sind)
        /// </summary>
        IPortfolio CurrentPortfolio { get; }

        /// <summary>
        /// gibt das Gewicht der Position als decimal zurück
        /// </summary>
        /// <param name="secid">die securitiy id</param>
        /// <param name="asof">der zeitpunkt</param>
        /// <returns></returns>
        decimal? GetWeight(int secid, DateTime? asof = null);

        /// <summary>
        /// gibt an ob zu dem Zeitpunkt ein aktuelles Investment vorlag
        /// </summary>
        /// <param name="secid">die securitiy id</param>
        /// <param name="asof">der zeitpunkt</param>
        /// <returns></returns>
        bool? IsActiveInvestment(int secid, DateTime? asof = null);

        /// <summary>
        /// gibt den Preis zurück
        /// </summary>
        /// <param name="secid">die securitiy id</param>
        /// <param name="asof">der zeitpunkt</param>
        /// <returns></returns>
        decimal? GetPrice(int secid, DateTime? asof = null);

        /// <summary>
        /// gibt den Preis auf Basis des Transactionstypen zurück
        /// </summary>
        /// <param name="secid">die securitiy id</param>
        /// <param name="transactionType">der Transaktionstype</param>
        /// <returns></returns>
        decimal? GetPrice(int secid, TransactionType transactionType);

        /// <summary>
        /// gibt den durchschnittlichen Preis zurück (wenn es mehrere Transaktionen zu einer Security gibt)
        /// </summary>
        /// <param name="secid">die securitiy id</param>
        /// <param name="asof">der Stichtag</param>
        /// <returns></returns>
        decimal? GetAveragePrice(int secid, DateTime asof);

        /// <summary>
        /// gibt das komplette TransactionItem zurück
        /// </summary>
        /// <param name="secId">die SecurityId</param>
        /// <param name="transactionType">der Transaktionstype</param>
        /// <param name="getLatest">flag ob das aktuellste item zurückgegebne werden soll</param>
        /// <returns></returns>
        ITransaction GetSingle(int secId, TransactionType? transactionType, bool getLatest = true);

        /// <summary>
        /// gibt die TransactionItems zurück
        /// </summary>
        /// <param name="secId">die SecurityId</param>
        /// <param name="activeOnly">das Flag das bestimmt ob nur aktive, also transaktionsgruppen
        /// die noch nicht geschlossen wurden, zurückgegeben wernden sollen</param>
        /// <param name="filter">der optionale Filter der mit gegeben werden kann</param>
        /// <returns></returns>
        IEnumerable<ITransaction> Get(int secId, bool activeOnly = false, Predicate<ITransaction> filter = null);

        /// <summary>
        /// Mehtode um den Transaktionsspeicher upzudaten
        /// </summary>
        void UpdateCache();

        /// <summary>
        /// methode um den Scoringprovider als Dependency zu injecten
        /// </summary>
        /// <param name="scoringProvider"></param>
        void RegisterScoringProvider(IScoringProvider scoringProvider);

        /// <summary>
        /// Gibt mir die aktuellen Holdings, das Current Portfolio zum Stichtag zurück
        /// </summary>
        /// <param name="asof">der Stichtag</param>
        /// <returns></returns>
        IEnumerable<ITransaction> GetCurrentHoldings(DateTime asof);

        /// <summary>
        /// Gibt mir die Transactionen von dem Stichtag zurück
        /// </summary>
        /// <param name="asof">der Stichtag</param>
        /// <returns></returns>
        IEnumerable<ITransaction> GetTransactions(DateTime asof);

        /// <summary>
        /// Gibt mir die aktuellen Shares aus dem Current Portfolio zurück, wenn die Position nicht im CurrentPortfolio ist dann null
        /// </summary>
        /// <param name="securityId"></param>
        /// <returns></returns>
        int? GetCurrentShares(int securityId);
    }
}