using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    /// <inheritdoc />
    /// <summary>
    /// Die Klasse die das Interface implementiert soll im Enumerator gleich die summierten Items zurückgeben 
    /// </summary>
    public interface ITemporaryPortfolio : IEnumerable<ITransaction>
    {
        /// <summary>
        /// Methode zum Adden
        /// </summary>
        /// <param name="item">die konkrete Transaktion</param>
        /// <param name="isTemporary"></param>
        void Add(ITransaction item, bool isTemporary = true);

        /// <summary>
        /// Methode zum Adden
        /// </summary>
        /// <param name="items">die konkreten Transaktionen</param>
        /// <param name="isTemporary"></param>
        void AddRange(IEnumerable<ITransaction> items, bool isTemporary = true);

        /// <summary>
        /// gibt zuurück ob die transaktion temporär ist
        /// </summary>
        /// <param name="secId">die SecId</param>
        /// <returns></returns>
        bool IsTemporary(int secId);

        /// <summary>
        /// Methode zum Speichern der Transaktionen
        /// </summary>
        /// <returns></returns>
        void SaveTransactions(ISaveProvider provider = null);

        /// <summary>
        /// Der Count
        /// </summary>
        int Count { get; }

        /// <summary>
        ///Flag das angibt ob es zu Änderungen gekommen ist
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        ///Löscht des Temporäre Portfolio
        /// </summary>
        void Clear();

        /// <summary>
        /// Gibt mir ein aktuelles Item aus dem Temporären Portdolio zurück
        /// </summary>
        /// <param name="candidateSecurityId"></param>
        /// <returns></returns>
        ITransaction Get(int candidateSecurityId);

        bool ContainsCandidate(ITradingCandidate temporaryCandidate);
    }
}