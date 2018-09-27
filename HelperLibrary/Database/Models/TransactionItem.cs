using HelperLibrary.Util.Atrributes;
using System;
using System.Data.Linq.Mapping;
using HelperLibrary.Enums;

namespace HelperLibrary.Database.Models
{
    public class TransactionItem
    {

        /// <summary>
        /// Der primary Key des Tables - Der Transaktions-Zeitpunkt
        /// </summary>
        [InputMapping(KeyWords = new[] { "DateTime", nameof(TransactionDateTime) })]
        [Column(Storage = "TRANSACTION_DATETIME")]
        public DateTime TransactionDateTime { get; set; }


        /// <summary>
        /// Der zweite primary Key des Tables - Die Security Id
        /// </summary>
        [InputMapping(KeyWords = new[] { "Id", nameof(SecurityId) })]
        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }


        /// <summary>
        /// Die Anzahl der Stücke
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(Shares) })]
        [Column(Storage = "SHARES")]
        public int Shares { get; set; }


        /// <summary>
        /// Der Gegenwert in EUR - Berechnet mit dem zuletzt verfügbaren Preis
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(TargetAmountEur), "Amount" })]
        [Column(Storage = "AMOUNT_EUR")]
        public decimal TargetAmountEur { get; set; }

        /// <summary>
        /// Der Typ der Transaktion (Opening,Closing,Changed) <see cref="HelperLibrary.Enums.TransactionType"/>
        /// </summary>
        [InputMapping(KeyWords = new[] { "Type", nameof(TransactionType) })]
        [Column(Storage = "TRANSACTION_TYPE")]
        public int TransactionType { get; set; }


        /// <summary>
        /// 1 bedeutet die Transaktion wurde gecancelled
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(Cancelled) })]
        [Column(Storage = "IS_CANCELLED")]
        public int Cancelled { get; set; }


        /// <summary>
        /// Das Zielgewicht der Position zum Stichtag im Portfolio
        /// </summary>
        [InputMapping(KeyWords = new[] { "target", nameof(TargetWeight) })]
        [Column(Storage = "TARGET_WEIGHT")]
        public decimal TargetWeight { get; set; }


        /// <summary>
        /// Das effektive Gewicht der Position zum Stichtag, sprich das effektive gewicht der einzelnen Transaktion
        /// </summary>
        [InputMapping(KeyWords = new[] { "Effective", nameof(EffectiveWeight) })]
        [Column(Storage = "EFFECTIVE_WEIGHT")]
        public decimal EffectiveWeight { get; set; }

        /// <summary>
        /// Der effektive Bertrag der Position, bei Verkäufen ist dieser negativ
        /// </summary>
        [InputMapping(KeyWords = new[] { "Effective Amount", nameof(EffectiveAmountEur) })]
        [Column(Storage = "EFFECTIVE_AMOUNTEUR")]
        public decimal EffectiveAmountEur { get; set; }


        /// <summary>
        /// Das Flag das angibt ob es sich um eine temporäre Transaktion handelt
        /// </summary>
        public bool IsTemporary { get; set; }

        /// <summary>
        /// Für Debugzwecke
        /// </summary>
        public string Name { get; set; }


        public override string ToString()
        {
            return $"TradeDate: {TransactionDateTime}_ID: {SecurityId}_Shares: {Shares}_Target Weight: {TargetWeight}_TransactionType: {(TransactionType)TransactionType} IsTemporary:{IsTemporary} {Name}";
        }


        public string UniqueKey => $"{TransactionDateTime.Date}_{SecurityId}_{Shares}_{(int)TransactionType}";
    }


}
