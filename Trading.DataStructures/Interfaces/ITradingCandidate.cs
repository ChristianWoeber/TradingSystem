using System;
using Trading.DataStructures.Enums;

namespace Trading.DataStructures.Interfaces
{
    public interface ITradingCandidate
    {
        /// <summary>
        /// Das Portfolio-Datum
        /// </summary>
        DateTime PortfolioAsof { get; set; }

        /// <summary>
        /// die letzte Transaktion
        /// </summary>
        ITransaction LastTransaction { get; set; }

        /// <summary>
        /// die aktuelle Position im Portfolio
        /// </summary>
        ITransaction CurrentPosition { get; set; }

        /// <summary>
        /// der aktuelle Score
        /// </summary>
        IScoringResult ScoringResult { get; }

        /// <summary>
        /// Der aktuelle Record
        /// </summary>
        ITradingRecord Record { get; }

        /// <summary>
        /// das Metadaten flag das angibt ob in dem Candidaten gerade ein aktives investment besteht 
        /// </summary>
        bool IsInvested { get; }

        /// <summary>
        /// das Metadaten flag das angibt ob der Candidate gerade im tempor#ren portfolio ist (also ein investment geplant ist) 
        /// </summary>
        bool IsTemporary { get; set; }

        /// <summary>
        /// das Metadaten flag das angibt, ob der Candidate ausgestoppt wurde 
        /// </summary>
        bool HasStopp { get; set; }

        /// <summary>
        /// das letzte Scoring Result 
        /// </summary>
        IScoringResult LastScoringResult { get; set; }

        /// <summary>
        /// Das neue Zielgewicht
        /// </summary>
        decimal TargetWeight { get; set; }

        /// <summary>
        /// das aktuelle gewicht im Portfolio
        /// </summary>
        decimal CurrentWeight { get; set; }

        /// <summary>
        /// der durchschnittspreis des Candidaten
        /// </summary>
        decimal AveragePrice { get; }

        /// <summary>
        /// der Typ der Transaktion
        /// </summary>
        TransactionType TransactionType { get; set; }

        /// <summary>
        /// Gibt an ob die Position unter dem Limit ist (die exekution des stopss kann aber aufgrund von lockups noch nach hinten verschoben werden)
        /// </summary>
        bool IsBelowStopp { get; set; }
    }
}