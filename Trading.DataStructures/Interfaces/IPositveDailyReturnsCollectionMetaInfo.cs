using System;

namespace Trading.DataStructures.Interfaces
{
    public interface IPositveDailyReturnsCollectionMetaInfo
    {
        /// <summary>
        /// die Anzahl der positiven Returns
        /// </summary>
        int Count { get; }

        /// <summary>
        /// das Datum des resten Positiven Returns
        /// </summary>
        IDailyReturnMetaInfo FirstItem { get; }

        /// <summary>
        /// Die Methode shifted die klasse eine periode weiter
        /// </summary>
        /// <param name="lastDailyReturn">der neue letzte Eintrag</param>
        /// <param name="firstDailyReturn">der neue erste Eintrage</param>
        void Shift(IDailyReturnMetaInfo lastDailyReturn, IDailyReturnMetaInfo firstDailyReturn);
    }
}