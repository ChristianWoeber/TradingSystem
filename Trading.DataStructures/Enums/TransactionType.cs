namespace Trading.DataStructures.Enums
{
    public enum TransactionType
    {
        /// <summary>
        /// Der Default Status
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Der Status für neu zu öffnende Positionen
        /// </summary>
        Open = 1,
        /// <summary>
        /// Der Status für zu schließende Positionen
        /// </summary>
        Close = 2,
        /// <summary>
        /// Der Status für ver änderte Positionen (aufbau / Abbau
        /// </summary>
        Changed = 3,
        /// <summary>
        /// Der Status für unveränderte Kandiaten => wenn er z.b.: noch in der minimum Holding period ist
        /// </summary>
        Unchanged =4
    }
}
