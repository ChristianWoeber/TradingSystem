﻿using System;
using HelperLibrary.Database.Models;
using HelperLibrary.Trading;

namespace HelperLibrary.Interfaces
{
    /// <summary>
    /// Interface Defining the StopLoss Settings
    /// </summary>
    public interface IStopLossSettings
    {
        /// <summary>
        /// der Wert um den die Position verringert wird
        /// </summary>
        double ReductionValue { get; }

        /// <summary>
        /// das StopLoss limit
        /// </summary>
        decimal LossLimit { get; set; }

        /// <summary>
        /// Methode für das Berechnen des LossLimits
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        bool HasStopLoss(TradingCandidate candidate);

        /// <summary>
        /// Die Methode berechnet die Limits auf täglicher basis
        /// </summary>
        /// <param name="transactionItem"></param>
        /// <param name="price"></param>
        /// <param name="portfolioAsof"></param>
        void UpdateDailyLimits(TransactionItem transactionItem, decimal? price, DateTime portfolioAsof);

        /// <summary>
        /// Die bekommt die transaktion und entscheidet auf Basis des Transaktionstypen ob sie hinzugefügt oder geadded wird
        /// </summary>
        /// <param name="transactionItem"></param>
        void AddOrRemoveDailyLimit(TransactionItem transactionItem);

        /// <summary>
        /// die mindesthaltedauer nachdem ein Stop ausgelöst wurde (soll gleich am nächsten Tag wieeder ein Stop ausgelöst werden? )
        /// </summary>
        int MinimumStopHoldingPeriodeInDays { get; set; }

        /// <summary>
        /// Gibt zurück ab welchem Time-Lag in tagen die Positon wider ausgestoppt werden darf 
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        bool IsBelowMinimumStopHoldingPeriod(TradingCandidate candidate);
    }
}