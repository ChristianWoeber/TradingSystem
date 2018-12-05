using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.DataStructures.Utils;

namespace Trading.Backtest.Data.Models
{
    public class TransactionEf : ITransaction
    {
        private string _uniqueKey;

        /// <summary>
        /// Der primary Key des Tables - Der Transaktions-Zeitpunkt
        /// </summary>
        [Key]
        public DateTime TransactionDateTime { get; set; }

        /// <summary>
        /// Der zweite primary Key des Tables - Die Security Id
        /// </summary>
        [Key]
        public int SecurityId { get; set; }

        /// <summary>
        /// Die Anzahl der Stücke
        /// </summary>
        public int Shares { get; set; }

        /// <summary>
        /// Der Gegenwert in EUR - Berechnet mit dem zuletzt verfügbaren Preis
        /// </summary>
        public decimal TargetAmountEur { get; set; }

        /// <summary>
        /// Der Typ der Transaktion (Opening,Closing,Changed) <see cref="DataStructures.Enums.TransactionType"/>
        /// </summary>     
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// 1 bedeutet die Transaktion wurde gecancelled
        /// </summary>
        public int Cancelled { get; set; }

        /// <summary>
        /// Das Zielgewicht der Position zum Stichtag im Portfolio
        /// </summary>    
        public decimal TargetWeight { get; set; }

        /// <summary>
        /// Das effektive Gewicht der Position zum Stichtag, sprich das effektive gewicht der einzelnen Transaktion
        /// </summary>
        public decimal EffectiveWeight { get; set; }

        /// <summary>
        /// Der effektive Bertrag der Position, bei Verkäufen ist dieser negativ
        /// </summary>     
        public decimal EffectiveAmountEur { get; set; }

        #region Unmapped Properties

        /// <summary>
        /// Das Flag das angibt ob es sich um eine temporäre Transaktion handelt
        /// </summary>
        [NotMapped]
        public bool IsTemporary { get; set; }

        /// <summary>
        /// Für Debugzwecke
        /// </summary>
        [NotMapped]
        public string Name { get; set; }

        [NotMapped]
        public string UniqueKey => _uniqueKey ?? (_uniqueKey = UniqueKeyProvider.CreateUniqueKey(this));

        public event EventHandler CancelledEvent;

        #endregion

        public override string ToString()
        {
            return $"TradeDate: {TransactionDateTime}_ID: {SecurityId}_Shares: {Shares}_Target Weight: {TargetWeight}_TransactionType: {TransactionType} IsTemporary:{IsTemporary} {Name}";
        }
    }
}
