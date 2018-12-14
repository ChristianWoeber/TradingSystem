using HelperLibrary.Util.Atrributes;
using System;
using System.Data.Linq.Mapping;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.DataStructures.Utils;

namespace HelperLibrary.Database.Models
{
    public class Transaction : ITransaction
    {
        private string _uniqueKey;
        private int _cancelled;

        /// <summary>
        /// Das Event das gefeuert wird wenn eine Transaktion gecancelled wird
        /// </summary>
        public event EventHandler CancelledEvent;

        /// <summary>
        /// Der primary Key des Tables - Der Transaktions-Zeitpunkt
        /// </summary>
        [InputMapping(KeyWords = new[] { "DateTime", nameof(TransactionDateTime) }, SortIndex = 1)]
        [Column(Storage = "TRANSACTION_DATETIME")]
        public DateTime TransactionDateTime { get; set; }


        /// <summary>
        /// Der zweite primary Key des Tables - Die Security Id
        /// </summary>
        [InputMapping(KeyWords = new[] { "Id", nameof(SecurityId) }, SortIndex = 2)]
        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }


        /// <summary>
        /// Die Anzahl der Stücke
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(Shares) }, SortIndex = 3)]
        [Column(Storage = "SHARES")]
        public int Shares { get; set; }


        /// <summary>
        /// Der Gegenwert in EUR - Berechnet mit dem zuletzt verfügbaren Preis
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(TargetAmountEur), "Amount" }, SortIndex = 4)]
        [Column(Storage = "AMOUNT_EUR")]
        public decimal TargetAmountEur { get; set; }

        /// <summary>
        /// Der Typ der Transaktion (Opening,Closing,Changed) <see cref="Trading.DataStructures.Enums.TransactionType"/>
        /// </summary>
        [InputMapping(KeyWords = new[] { "Type", nameof(TransactionType) }, SortIndex = 5)]
        [Column(Storage = "TRANSACTION_TYPE")]
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// 1 bedeutet die Transaktion wurde gecancelled
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(Cancelled) }, SortIndex = 6)]
        [Column(Storage = "IS_CANCELLED")]
        public int Cancelled
        {
            get => _cancelled;
            set
            {
                if (value == _cancelled)
                    return;
                _cancelled = value;
                CancelledEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Das Zielgewicht der Position zum Stichtag im Portfolio
        /// </summary>
        [InputMapping(KeyWords = new[] { "target", nameof(TargetWeight) }, SortIndex = 7)]
        [Column(Storage = "TARGET_WEIGHT")]
        public decimal TargetWeight { get; set; }


        /// <summary>
        /// Das effektive Gewicht der Position zum Stichtag, sprich das effektive gewicht der einzelnen Transaktion
        /// </summary>
        [InputMapping(KeyWords = new[] { "Effective", nameof(EffectiveWeight) }, SortIndex = 8)]
        [Column(Storage = "EFFECTIVE_WEIGHT")]
        public decimal EffectiveWeight { get; set; }

        /// <summary>
        /// Der effektive Bertrag der Position, bei Verkäufen ist dieser negativ
        /// </summary>
        [InputMapping(KeyWords = new[] { "Effective Amount", nameof(EffectiveAmountEur) }, SortIndex = 9)]
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
            return $"TradeDate: {TransactionDateTime}_ID: {SecurityId}_Shares: {Shares}_Target Weight: {TargetWeight}_EffectiveWeight: {EffectiveWeight}_EffectiveAmountEur: {EffectiveAmountEur}_TransactionType: {TransactionType}_IsTemporary:{IsTemporary}";
        }

        public string UniqueKey => _uniqueKey ?? (_uniqueKey = UniqueKeyProvider.CreateUniqueKey(this));


    }


}
