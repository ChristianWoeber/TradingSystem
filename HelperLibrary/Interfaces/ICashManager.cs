using System;

namespace HelperLibrary.Interfaces
{
    public interface ICashManager
    {
        /// <summary>
        ///Methode gibt das noch zur Veranlagung stehende Cash zurück
        /// </summary>
        bool TryHasCash(out decimal remainingCash);

        /// <summary>
        /// der aktuelle Cash Bestand
        /// </summary>
        decimal Cash { get; set; }

        /// <summary>
        /// Das event das gefeuert wird wenn sich der Cash Value ändert
        /// </summary>
        event EventHandler CashChangedEvent;
    }
}