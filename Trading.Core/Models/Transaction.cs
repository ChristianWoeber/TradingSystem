using System;
using System.Data.Linq.Mapping;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.DataStructures.Utils;
using Trading.Parsing.Attributes;

namespace Trading.Core.Models
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
        /// die Ticket Fee pro Trade
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(TicketFee) }, SortIndex = 10)]
        [Column(Storage = "TICKET_FEE")]
        public decimal TicketFee { get; set; }

        /// <summary>
        /// Der primary Key des Tables - Der Transaktions-Zeitpunkt
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(TransactionDateTime), nameof(DateTime) }, SortIndex = 1)]
        [Column(Storage = "TRANSACTION_DATETIME")]
        public DateTime TransactionDateTime { get; set; }


        /// <summary>
        /// Der zweite primary Key des Tables - Die Security Id
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(SecurityId) }, SortIndex = 2)]
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
        [InputMapping(KeyWords = new[] { nameof(TargetAmountEur), "AmountEur" }, SortIndex = 4)]
        [Column(Storage = "AMOUNT_EUR")]
        public decimal TargetAmountEur { get; set; }

        /// <summary>
        /// Der Typ der Transaktion (Opening,Closing,Changed) <see cref="Trading.DataStructures.Enums.TransactionType"/>
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(TransactionType),"Type" }, SortIndex = 5)]
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
        [InputMapping(KeyWords = new[] { nameof(TargetWeight) }, SortIndex = 7)]
        [Column(Storage = "TARGET_WEIGHT")]
        public decimal TargetWeight { get; set; }


        /// <summary>
        /// Das effektive Gewicht der Position zum Stichtag, sprich das effektive gewicht der einzelnen Transaktion
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(EffectiveWeight) }, SortIndex = 8)]
        [Column(Storage = "EFFECTIVE_WEIGHT")]
        public decimal EffectiveWeight { get; set; }

        /// <summary>
        /// Der effektive Bertrag der Position, bei Verkäufen ist dieser negativ
        /// </summary>
        [InputMapping(KeyWords = new[] { nameof(EffectiveAmountEur) }, SortIndex = 9)]
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

        /// <summary>
        /// Einen Dummy Close Transaction als Hotfix
        /// </summary>
        /// <param name="endDateTime">das Datum der Transaktion</param>
        /// <param name="opening">die Opening Transaction</param>
        /// <returns></returns>
        public static ITransaction CreateCloseDummy(DateTime endDateTime, ITransaction opening)
        {
            //ich übernehme die Transactionsdetails des openings und ändere nur das Datum
            var trans = opening;
            trans.TransactionDateTime = endDateTime;
            return trans;
        }
    }


}
