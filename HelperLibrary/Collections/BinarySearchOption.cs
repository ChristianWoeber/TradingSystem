namespace HelperLibrary.Collections
{
    public enum BinarySearchOption
    {
        /// <summary>
        /// Gibt einen leeren Wert, oder -1 (bei Index) zurück, wenn der Key nicht gefunden wurde
        /// </summary>
        GetInvalidIfNotFound = 0,

        /// <summary>
        /// Gibt den vorhergegangenen Wert zurück, wenn der Key nicht gefunden wurde.
        /// </summary>
        GetLastIfNotFound = 1,

        /// <summary>
        /// Gibt den nächsten Wert zurück, wenn der Key nicht gefunden wurde.
        /// </summary>
        GetNextIfNotFound = 2,

        /// <summary>
        /// Wirft eine KeyNotFound Exception wenn der Key nicht gefunden wurde
        /// </summary>
        GetExceptionIfNotFound = 4,
    }
}