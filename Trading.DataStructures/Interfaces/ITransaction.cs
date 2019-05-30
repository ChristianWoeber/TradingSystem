using System;
using Trading.DataStructures.Enums;

namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// Durch das Interface können Klassen identifierziert werden, für die,
    /// die property infos und die mappings infos beim erstellen des Konstructors einmalig gesammelt werden sollen
    /// </summary>
    public interface IInputMappable
    {
    }

    public interface ITransaction : IInputMappable
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
        /// Der Typ der Transaktion (Opening,Closing,Changed) <see cref="Enums.TransactionType"/>
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

        /// <summary>
        /// Gibt an ob die Transaktion im temporören Portfolio neu ist (in diese wird versucht zu investeiren)
        /// </summary>
        bool IsTemporary { get; set; }

        /// <summary>
        /// Der eindeutige Key der Transaktion
        /// </summary>
        string UniqueKey { get; }

        /// <summary>
        /// Das Event das gefeuert wird wenn eine Transaktion gecancelled wird
        /// </summary>

        event EventHandler CancelledEvent;

        /// <summary>
        /// die Ticket Fee pro Trade
        /// </summary>
        decimal TicketFee { get; set; }

    }
}