using System;
using Trading.DataStructures.Enums;

namespace Trading.Backtest.Data.Models
{
    public interface ITransaction
    {
        /// <summary>
        /// Der primary Key des Tables - Der Transaktions-Zeitpunkt
        /// </summary>
        DateTime TransactionDateTime { get; set; }

        /// <summary>
        /// Der zweite primary Key des Tables - Die Security Id
        /// </summary>
        int SecurityId { get; set; }

        /// <summary>
        /// Die Anzahl der Stücke
        /// </summary>
        int Shares { get; set; }

        /// <summary>
        /// Der Gegenwert in EUR - Berechnet mit dem zuletzt verfügbaren Preis
        /// </summary>
        decimal TargetAmountEur { get; set; }

        /// <summary>
        /// Der Typ der Transaktion (Opening,Closing,Changed) <see cref="DataStructures.Enums.TransactionType"/>
        /// </summary>     
        TransactionType TransactionType { get; set; }

        /// <summary>
        /// 1 bedeutet die Transaktion wurde gecancelled
        /// </summary>
        int Cancelled { get; set; }

        /// <summary>
        /// Das Zielgewicht der Position zum Stichtag im Portfolio
        /// </summary>    
        decimal TargetWeight { get; set; }

        /// <summary>
        /// Das effektive Gewicht der Position zum Stichtag, sprich das effektive gewicht der einzelnen Transaktion
        /// </summary>
        decimal EffectiveWeight { get; set; }

        /// <summary>
        /// Der effektive Bertrag der Position, bei Verkäufen ist dieser negativ
        /// </summary>     
        decimal EffectiveAmountEur { get; set; }
    }
}